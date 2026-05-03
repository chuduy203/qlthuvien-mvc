using System.Text.Json;
using LibraryManagement.Data;
using LibraryManagement.Extensions;
using LibraryManagement.Filters;
using LibraryManagement.Models;
using LibraryManagement.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Controllers;

[RoleAuthorize("Admin", "Librarian")]
public class BorrowTicketsController : Controller
{
    private readonly LibraryDbContext _context;
    private readonly IConfiguration _configuration;

    public BorrowTicketsController(LibraryDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<IActionResult> Index(string? status)
    {
        var query = _context.BorrowTickets
            .Include(x => x.Reader)
            .Include(x => x.User)
            .Include(x => x.BorrowTicketDetails)
            .ThenInclude(x => x.Book)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.Status == status);
        }

        ViewBag.Status = status;
        return View(await query.OrderByDescending(x => x.Id).ToListAsync());
    }

    public async Task<IActionResult> Create()
    {
        var borrowDays = _configuration.GetValue<int>("AppSettings:DefaultBorrowDays");
        var model = new BorrowTicketCreateViewModel
        {
            BorrowDate = DateTime.Today,
            DueDate = DateTime.Today.AddDays(borrowDays > 0 ? borrowDays : 14)
        };

        await LoadCreateFormDataAsync(model.ReaderId, model.Items.Select(x => x.BookId));
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BorrowTicketCreateViewModel model)
    {
        model.Items = model.Items
            .Where(x => x.BookId > 0 && x.Quantity > 0)
            .ToList();

        if (model.Items.Count == 0)
        {
            ModelState.AddModelError(string.Empty, this.L("Vui lòng chọn ít nhất 1 sách.", "Please select at least 1 book."));
        }

        if (model.DueDate.Date < model.BorrowDate.Date)
        {
            ModelState.AddModelError(nameof(model.DueDate), this.L("Ngày trả dự kiến phải lớn hơn hoặc bằng ngày mượn.", "Due date must be greater than or equal to borrow date."));
        }

        var readerExists = await _context.Readers.AnyAsync(x => x.Id == model.ReaderId && x.IsActive);
        if (!readerExists)
        {
            ModelState.AddModelError(nameof(model.ReaderId), this.L("Độc giả không tồn tại hoặc đang bị khóa.", "Reader does not exist or is inactive."));
        }

        var groupedItems = model.Items
            .GroupBy(x => x.BookId)
            .Select(g => new { BookId = g.Key, Quantity = g.Sum(x => x.Quantity) })
            .ToList();

        var bookIds = groupedItems.Select(x => x.BookId).ToList();
        var books = await _context.Books.Where(x => bookIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id);

        foreach (var item in groupedItems)
        {
            if (!books.TryGetValue(item.BookId, out var book))
            {
                ModelState.AddModelError(string.Empty, this.L($"Sách ID {item.BookId} không tồn tại.", $"Book ID {item.BookId} does not exist."));
                continue;
            }

            if (book.AvailableQuantity < item.Quantity)
            {
                ModelState.AddModelError(
                    string.Empty,
                    this.L(
                        $"Sách '{book.BookName}' chỉ còn {book.AvailableQuantity} cuốn.",
                        $"Book '{book.BookName}' has only {book.AvailableQuantity} copies left."
                    )
                );
            }
        }

        if (!ModelState.IsValid)
        {
            if (model.Items.Count == 0)
            {
                model.Items.Add(new BorrowTicketBookItemViewModel());
            }
            await LoadCreateFormDataAsync(model.ReaderId, model.Items.Select(x => x.BookId));
            return View(model);
        }

        var userId = HttpContext.Session.GetInt32("UserId") ?? 1;
        var ticket = new BorrowTicket
        {
            BorrowCode = "PM" + DateTime.Now.ToString("yyyyMMddHHmmss"),
            ReaderId = model.ReaderId,
            UserId = userId,
            BorrowDate = model.BorrowDate.Date,
            DueDate = model.DueDate.Date,
            Status = "Borrowing",
            TotalRentalFee = 0
        };

        _context.BorrowTickets.Add(ticket);
        await _context.SaveChangesAsync();

        decimal totalRentalFee = 0;
        foreach (var item in groupedItems)
        {
            var book = books[item.BookId];
            var lineFee = book.RentalFee * item.Quantity;
            totalRentalFee += lineFee;

            _context.BorrowTicketDetails.Add(new BorrowTicketDetail
            {
                BorrowTicketId = ticket.Id,
                BookId = item.BookId,
                Quantity = item.Quantity,
                UnitRentalFee = book.RentalFee,
                LineRentalFee = lineFee
            });

            book.AvailableQuantity -= item.Quantity;
        }

        ticket.TotalRentalFee = totalRentalFee;
        await _context.SaveChangesAsync();

        TempData["Success"] = this.L(
            $"Đã lập phiếu mượn thành công. Tổng phí thuê: {totalRentalFee:N0} VND",
            $"Borrow ticket created successfully. Total rental fee: {totalRentalFee:N0} VND"
        );
        return RedirectToAction(nameof(Print), new { id = ticket.Id });
    }

    public async Task<IActionResult> Print(int id)
    {
        var ticket = await _context.BorrowTickets
            .Include(x => x.Reader)
            .Include(x => x.User)
            .Include(x => x.BorrowTicketDetails)
            .ThenInclude(x => x.Book)
            .FirstOrDefaultAsync(x => x.Id == id);

        return ticket == null ? NotFound() : View(ticket);
    }

    private async Task LoadCreateFormDataAsync(int? readerId = null, IEnumerable<int>? selectedBookIds = null)
    {
        ViewBag.Readers = new SelectList(
            await _context.Readers.Where(x => x.IsActive).OrderBy(x => x.FullName).ToListAsync(),
            "Id",
            "FullName",
            readerId);

        var selectedIds = (selectedBookIds ?? Enumerable.Empty<int>()).Where(x => x > 0).Distinct().ToList();
        var books = await _context.Books
            .Where(x => x.AvailableQuantity > 0 || selectedIds.Contains(x.Id))
            .OrderBy(x => x.BookName)
            .Select(x => new
            {
                x.Id,
                DisplayName = x.BookName + " (" + x.BookCode + ")",
                x.AvailableQuantity,
                x.RentalFee
            })
            .ToListAsync();

        ViewBag.Books = new SelectList(books, "Id", "DisplayName");
        ViewBag.BooksJson = JsonSerializer.Serialize(books.Select(x => new
        {
            id = x.Id,
            name = x.DisplayName,
            availableQuantity = x.AvailableQuantity,
            rentalFee = x.RentalFee
        }));
    }
}
