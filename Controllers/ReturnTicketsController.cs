using System.Data;
using LibraryManagement.Data;
using LibraryManagement.Extensions;
using LibraryManagement.Filters;
using LibraryManagement.Models;
using LibraryManagement.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Controllers;

[RoleAuthorize("Admin", "Librarian")]
public class ReturnTicketsController : Controller
{
    private const string ReturnedStatus = "Returned";

    private readonly LibraryDbContext _context;
    private readonly IConfiguration _configuration;

    public ReturnTicketsController(LibraryDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<IActionResult> Index()
    {
        var data = await _context.ReturnTickets
            .Include(x => x.BorrowTicket)
            .ThenInclude(x => x!.Reader)
            .Include(x => x.ReturnTicketDetails)
            .ThenInclude(x => x.Book)
            .OrderByDescending(x => x.Id)
            .ToListAsync();

        var borrowTicketIds = data.Select(x => x.BorrowTicketId).Distinct().ToList();
        var fineTotals = await _context.Fines
            .Where(x => borrowTicketIds.Contains(x.BorrowTicketId))
            .GroupBy(x => x.BorrowTicketId)
            .Select(g => new
            {
                BorrowTicketId = g.Key,
                Total = g.Sum(x => x.Amount)
            })
            .ToDictionaryAsync(x => x.BorrowTicketId, x => x.Total);

        ViewBag.FineTotals = fineTotals;
        return View(data);
    }

    public async Task<IActionResult> ReturnBook(int borrowTicketId)
    {
        var borrowTicket = await GetBorrowTicketForReturnAsync(borrowTicketId);
        if (borrowTicket == null)
        {
            return NotFound();
        }

        if (borrowTicket.Status == ReturnedStatus)
        {
            TempData["Error"] = this.L("Phiếu mượn này đã được trả trước đó.", "This borrow ticket has already been returned.");
            return RedirectToAction("Index", "BorrowTickets");
        }

        var model = BuildReturnModel(borrowTicket);
        SetFineConfigToView();
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ReturnBook(ReturnBookProcessViewModel model)
    {
        model.Items ??= [];

        var borrowTicket = await GetBorrowTicketForReturnAsync(model.BorrowTicketId);
        if (borrowTicket == null)
        {
            return NotFound();
        }

        if (borrowTicket.Status == ReturnedStatus)
        {
            TempData["Error"] = this.L("Phiếu mượn này đã được trả trước đó.", "This borrow ticket has already been returned.");
            return RedirectToAction("Index", "BorrowTickets");
        }

        ValidateReturnModel(model, borrowTicket);
        if (!ModelState.IsValid)
        {
            FillReadOnlyFields(model, borrowTicket);
            SetFineConfigToView();
            return View(model);
        }

        var (finePerDay, damageCompensationRate, lostCompensationRate) = GetFineConfig();
        var userId = HttpContext.Session.GetInt32("UserId") ?? 1;

        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var returnTicket = new ReturnTicket
            {
                ReturnCode = "PT" + DateTime.Now.ToString("yyyyMMddHHmmssfff"),
                BorrowTicketId = borrowTicket.Id,
                ReturnDate = model.ReturnDate.Date,
                UserId = userId
            };

            _context.ReturnTickets.Add(returnTicket);
            await _context.SaveChangesAsync();

            var borrowDetailMap = borrowTicket.BorrowTicketDetails.ToDictionary(x => x.BookId);
            decimal damageFineAmount = 0;
            decimal lostFineAmount = 0;

            foreach (var item in model.Items)
            {
                var detail = borrowDetailMap[item.BookId];
                var book = detail.Book!;

                _context.ReturnTicketDetails.Add(new ReturnTicketDetail
                {
                    ReturnTicketId = returnTicket.Id,
                    BookId = item.BookId,
                    Quantity = detail.Quantity,
                    NormalQuantity = item.NormalQuantity,
                    DamagedQuantity = item.DamagedQuantity,
                    LostQuantity = item.LostQuantity,
                    IsDamaged = item.DamagedQuantity > 0 || item.LostQuantity > 0
                });

                book.AvailableQuantity += item.NormalQuantity;

                var damagedAndLost = item.DamagedQuantity + item.LostQuantity;
                if (damagedAndLost > 0)
                {
                    book.Quantity = Math.Max(0, book.Quantity - damagedAndLost);
                }

                var bookValue = book.BookPrice > 0 ? book.BookPrice : Math.Max(0m, detail.UnitRentalFee);
                var itemDamageFine = RoundVnd(item.DamagedQuantity * bookValue * damageCompensationRate);
                var itemLostFine = RoundVnd(item.LostQuantity * bookValue * lostCompensationRate);
                damageFineAmount += itemDamageFine;
                lostFineAmount += itemLostFine;
            }

            borrowTicket.Status = ReturnedStatus;

            var lateDays = model.ReturnDate.Date > borrowTicket.DueDate.Date
                ? (model.ReturnDate.Date - borrowTicket.DueDate.Date).Days
                : 0;

            if (lateDays > 0)
            {
                _context.Fines.Add(new Fine
                {
                    ReaderId = borrowTicket.ReaderId,
                    BorrowTicketId = borrowTicket.Id,
                    LateDays = lateDays,
                    Amount = lateDays * finePerDay,
                    Reason = this.L($"Trả sách quá hạn {lateDays} ngày", $"Overdue return by {lateDays} day(s)"),
                    IsPaid = false
                });
            }

            if (damageFineAmount > 0)
            {
                _context.Fines.Add(new Fine
                {
                    ReaderId = borrowTicket.ReaderId,
                    BorrowTicketId = borrowTicket.Id,
                    LateDays = 0,
                    Amount = damageFineAmount,
                    Reason = this.L("Phạt làm rách/hỏng sách", "Fine for damaged book"),
                    IsPaid = false
                });
            }

            if (lostFineAmount > 0)
            {
                _context.Fines.Add(new Fine
                {
                    ReaderId = borrowTicket.ReaderId,
                    BorrowTicketId = borrowTicket.Id,
                    LateDays = 0,
                    Amount = lostFineAmount,
                    Reason = this.L("Phạt làm mất sách", "Fine for lost book"),
                    IsPaid = false
                });
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var lateFineAmount = lateDays > 0 ? lateDays * finePerDay : 0;
            var totalFine = lateFineAmount + damageFineAmount + lostFineAmount;
            TempData["Success"] = totalFine > 0
                ? this.L($"Trả sách thành công. Tổng tiền phạt: {totalFine:N0} VND.", $"Books returned successfully. Total fine: {totalFine:N0} VND.")
                : this.L("Trả sách thành công.", "Books returned successfully.");

            return RedirectToAction("Index", "BorrowTickets");
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            TempData["Error"] = this.L("Không thể lưu phiếu trả. Vui lòng thử lại.", "Failed to save return ticket. Please try again.");
            return RedirectToAction("Index", "BorrowTickets");
        }
    }

    private async Task<BorrowTicket?> GetBorrowTicketForReturnAsync(int borrowTicketId)
    {
        return await _context.BorrowTickets
            .Include(x => x.Reader)
            .Include(x => x.BorrowTicketDetails)
            .ThenInclude(x => x.Book)
            .FirstOrDefaultAsync(x => x.Id == borrowTicketId);
    }

    private ReturnBookProcessViewModel BuildReturnModel(BorrowTicket borrowTicket)
    {
        return new ReturnBookProcessViewModel
        {
            BorrowTicketId = borrowTicket.Id,
            BorrowCode = borrowTicket.BorrowCode,
            ReaderName = borrowTicket.Reader?.FullName ?? string.Empty,
            BorrowDate = borrowTicket.BorrowDate,
            DueDate = borrowTicket.DueDate,
            ReturnDate = DateTime.Today,
            Items = borrowTicket.BorrowTicketDetails
                .Select(x => new ReturnBookItemViewModel
                {
                    BookId = x.BookId,
                    BookCode = x.Book?.BookCode ?? string.Empty,
                    BookName = x.Book?.BookName ?? string.Empty,
                    BorrowedQuantity = x.Quantity,
                    NormalQuantity = x.Quantity,
                    DamagedQuantity = 0,
                    LostQuantity = 0,
                    RentalFee = x.UnitRentalFee,
                    BookPrice = x.Book != null && x.Book.BookPrice > 0 ? x.Book.BookPrice : x.UnitRentalFee
                })
                .ToList()
        };
    }

    private void FillReadOnlyFields(ReturnBookProcessViewModel model, BorrowTicket borrowTicket)
    {
        model.Items ??= [];
        model.BorrowCode = borrowTicket.BorrowCode;
        model.ReaderName = borrowTicket.Reader?.FullName ?? string.Empty;
        model.BorrowDate = borrowTicket.BorrowDate;
        model.DueDate = borrowTicket.DueDate;

        if (model.Items.Count == 0)
        {
            model.Items = borrowTicket.BorrowTicketDetails
                .Select(x => new ReturnBookItemViewModel
                {
                    BookId = x.BookId,
                    BookCode = x.Book?.BookCode ?? string.Empty,
                    BookName = x.Book?.BookName ?? string.Empty,
                    BorrowedQuantity = x.Quantity,
                    NormalQuantity = x.Quantity,
                    DamagedQuantity = 0,
                    LostQuantity = 0,
                    RentalFee = x.UnitRentalFee,
                    BookPrice = x.Book != null && x.Book.BookPrice > 0 ? x.Book.BookPrice : x.UnitRentalFee
                })
                .ToList();
            return;
        }

        var detailMap = borrowTicket.BorrowTicketDetails.ToDictionary(x => x.BookId);
        foreach (var item in model.Items)
        {
            if (!detailMap.TryGetValue(item.BookId, out var detail))
            {
                continue;
            }

            item.BookCode = detail.Book?.BookCode ?? string.Empty;
            item.BookName = detail.Book?.BookName ?? string.Empty;
            item.BorrowedQuantity = detail.Quantity;
            item.RentalFee = detail.UnitRentalFee;
            item.BookPrice = detail.Book != null && detail.Book.BookPrice > 0 ? detail.Book.BookPrice : detail.UnitRentalFee;
        }
    }

    private void ValidateReturnModel(ReturnBookProcessViewModel model, BorrowTicket borrowTicket)
    {
        if (model.ReturnDate.Date < borrowTicket.BorrowDate.Date)
        {
            ModelState.AddModelError(nameof(model.ReturnDate), this.L("Ngày trả không hợp lệ.", "Invalid return date."));
        }

        if (model.Items.Count == 0)
        {
            ModelState.AddModelError(string.Empty, this.L("Không có dữ liệu sách trả.", "No return item data."));
            return;
        }

        var detailMap = borrowTicket.BorrowTicketDetails.ToDictionary(x => x.BookId);
        var duplicateBookIds = model.Items
            .GroupBy(x => x.BookId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateBookIds.Count > 0)
        {
            ModelState.AddModelError(string.Empty, this.L("Dữ liệu sách trả bị trùng.", "Duplicate return book entries found."));
        }

        if (model.Items.Count != detailMap.Count || model.Items.Any(x => !detailMap.ContainsKey(x.BookId)))
        {
            ModelState.AddModelError(string.Empty, this.L("Danh sách sách trả không khớp với phiếu mượn.", "Return book list does not match borrow ticket."));
        }

        for (var i = 0; i < model.Items.Count; i++)
        {
            var item = model.Items[i];
            if (!detailMap.TryGetValue(item.BookId, out var detail))
            {
                ModelState.AddModelError(string.Empty, this.L("Dữ liệu sách trả không hợp lệ.", "Invalid return book data."));
                continue;
            }

            var processed = item.NormalQuantity + item.DamagedQuantity + item.LostQuantity;
            if (processed < 0)
            {
                ModelState.AddModelError($"Items[{i}].NormalQuantity", this.L("Số lượng xử lý không hợp lệ.", "Invalid processed quantity."));
                continue;
            }

            if (processed != detail.Quantity)
            {
                ModelState.AddModelError(
                    $"Items[{i}].NormalQuantity",
                    this.L(
                        $"Sách '{detail.Book?.BookName}' phải có tổng số lượng xử lý đúng bằng {detail.Quantity}.",
                        $"Book '{detail.Book?.BookName}' must have processed quantity equal to {detail.Quantity}."
                    )
                );
            }
        }
    }

    private decimal? GetPositiveDecimal(string configKey)
    {
        var value = _configuration.GetValue<decimal?>(configKey);
        return value.HasValue && value.Value > 0 ? value.Value : null;
    }

    private (decimal FinePerDay, decimal DamageCompensationRate, decimal LostCompensationRate) GetFineConfig()
    {
        var finePerDay = GetPositiveDecimal("AppSettings:FinePerDay") ?? 5000m;
        var damageCompensationRate = GetPositiveDecimal("AppSettings:DamageCompensationRate")
                                     ?? GetPositiveDecimal("AppSettings:DamageFineMultiplier")
                                     ?? 0.3m;
        var lostCompensationRate = GetPositiveDecimal("AppSettings:LostCompensationRate")
                                   ?? GetPositiveDecimal("AppSettings:LostFineMultiplier")
                                   ?? 1m;
        return (finePerDay, damageCompensationRate, lostCompensationRate);
    }

    private void SetFineConfigToView()
    {
        var (finePerDay, damageCompensationRate, lostCompensationRate) = GetFineConfig();
        ViewBag.FinePerDay = finePerDay;
        ViewBag.DamageCompensationRate = damageCompensationRate;
        ViewBag.LostCompensationRate = lostCompensationRate;
    }

    private static decimal RoundVnd(decimal amount)
    {
        return Math.Round(amount, 0, MidpointRounding.AwayFromZero);
    }
}
