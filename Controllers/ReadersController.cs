using LibraryManagement.Data;
using LibraryManagement.Extensions;
using LibraryManagement.Filters;
using LibraryManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Controllers;

[RoleAuthorize("Admin", "Librarian")]
public class ReadersController : Controller
{
    private readonly LibraryDbContext _context;

    public ReadersController(LibraryDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? keyword, int page = 1)
    {
        const int pageSize = 10;
        var query = _context.Readers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(x =>
                x.FullName.Contains(keyword) ||
                x.ReaderCode.Contains(keyword) ||
                (x.Phone != null && x.Phone.Contains(keyword)) ||
                (x.IdentityNumber != null && x.IdentityNumber.Contains(keyword)));
        }

        ViewBag.Keyword = keyword;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(await query.CountAsync() / (double)pageSize);

        var data = await query
            .OrderByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return View(data);
    }

    public IActionResult Create()
    {
        return View(new Reader());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Reader model)
    {
        model.ReaderCode = model.ReaderCode?.Trim() ?? string.Empty;
        model.FullName = model.FullName?.Trim() ?? string.Empty;
        model.IdentityNumber = string.IsNullOrWhiteSpace(model.IdentityNumber) ? null : model.IdentityNumber.Trim();

        await ValidateReaderAsync(model);
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        _context.Readers.Add(model);
        await _context.SaveChangesAsync();
        TempData["Success"] = this.L("Đã thêm độc giả thành công.", "Reader added successfully.");
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var model = await _context.Readers.FindAsync(id);
        return model == null ? NotFound() : View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Reader model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        model.ReaderCode = model.ReaderCode?.Trim() ?? string.Empty;
        model.FullName = model.FullName?.Trim() ?? string.Empty;
        model.IdentityNumber = string.IsNullOrWhiteSpace(model.IdentityNumber) ? null : model.IdentityNumber.Trim();

        var entity = await _context.Readers.FindAsync(id);
        if (entity == null)
        {
            return NotFound();
        }

        await ValidateReaderAsync(model);
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        entity.ReaderCode = model.ReaderCode.Trim();
        entity.FullName = model.FullName.Trim();
        entity.Email = model.Email;
        entity.Phone = model.Phone;
        entity.Address = model.Address;
        entity.DateOfBirth = model.DateOfBirth;
        entity.IdentityNumber = model.IdentityNumber;
        entity.Gender = model.Gender;
        entity.Occupation = model.Occupation;
        entity.Workplace = model.Workplace;
        entity.MembershipType = model.MembershipType;
        entity.CardIssuedDate = model.CardIssuedDate;
        entity.CardExpiryDate = model.CardExpiryDate;
        entity.EmergencyContactName = model.EmergencyContactName;
        entity.EmergencyContactPhone = model.EmergencyContactPhone;
        entity.Notes = model.Notes;
        entity.IsActive = model.IsActive;

        await _context.SaveChangesAsync();
        TempData["Success"] = this.L("Đã cập nhật độc giả thành công.", "Reader updated successfully.");
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> History(int id)
    {
        var reader = await _context.Readers.FindAsync(id);
        if (reader == null)
        {
            return NotFound();
        }

        ViewBag.Reader = reader;
        var tickets = await _context.BorrowTickets
            .Include(x => x.BorrowTicketDetails)
            .ThenInclude(x => x.Book)
            .Where(x => x.ReaderId == id)
            .OrderByDescending(x => x.Id)
            .ToListAsync();

        return View(tickets);
    }

    private async Task ValidateReaderAsync(Reader model)
    {
        if (await _context.Readers.AnyAsync(x => x.ReaderCode == model.ReaderCode && x.Id != model.Id))
        {
            ModelState.AddModelError(nameof(model.ReaderCode), this.L("Mã độc giả đã tồn tại.", "Reader code already exists."));
        }

        if (!string.IsNullOrWhiteSpace(model.IdentityNumber) &&
            await _context.Readers.AnyAsync(x => x.IdentityNumber == model.IdentityNumber && x.Id != model.Id))
        {
            ModelState.AddModelError(nameof(model.IdentityNumber), this.L("Số CCCD/CMND đã tồn tại.", "National ID already exists."));
        }

        if (model.CardIssuedDate.HasValue && model.CardExpiryDate.HasValue &&
            model.CardExpiryDate.Value.Date < model.CardIssuedDate.Value.Date)
        {
            ModelState.AddModelError(
                nameof(model.CardExpiryDate),
                this.L("Ngày hết hạn thẻ phải lớn hơn hoặc bằng ngày cấp.", "Card expiry date must be greater than or equal to issue date.")
            );
        }
    }
}
