using LibraryManagement.Data;
using LibraryManagement.Extensions;
using LibraryManagement.Filters;
using LibraryManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Controllers;

[RoleAuthorize("Admin", "Librarian")]
public class BooksController : Controller
{
    private readonly LibraryDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".gif", ".webp"];

    public BooksController(LibraryDbContext context, IWebHostEnvironment environment, IConfiguration configuration)
    {
        _context = context;
        _environment = environment;
        _configuration = configuration;
    }

    public async Task<IActionResult> Index(string? keyword, int? categoryId, int page = 1)
    {
        const int pageSize = 5;
        var query = _context.Books.Include(x => x.Category).Include(x => x.Author).Include(x => x.Publisher).AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(x =>
                x.BookName.Contains(keyword) ||
                x.BookCode.Contains(keyword) ||
                x.Author!.AuthorName.Contains(keyword));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(x => x.CategoryId == categoryId.Value);
        }

        var totalItems = await query.CountAsync();
        var books = await query.OrderByDescending(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        ViewBag.Keyword = keyword;
        ViewBag.CategoryId = categoryId;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "CategoryName");
        return View(books);
    }

    [RoleAuthorize("Admin")]
    public async Task<IActionResult> Create()
    {
        await LoadDropdowns();
        return View(new Book());
    }

    [RoleAuthorize("Admin")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Book book, IFormFile? imageFile)
    {
        book.BookCode = book.BookCode?.Trim() ?? string.Empty;
        book.BookName = book.BookName?.Trim() ?? string.Empty;

        if (await _context.Books.AnyAsync(x => x.BookCode == book.BookCode))
        {
            ModelState.AddModelError(nameof(book.BookCode), this.L("Mã sách đã tồn tại", "Book code already exists"));
        }

        if (!ModelState.IsValid)
        {
            await LoadDropdowns();
            return View(book);
        }

        book.AvailableQuantity = book.Quantity;
        if (imageFile != null)
        {
            book.ImageUrl = await SaveBookImage(imageFile);
        }

        _context.Books.Add(book);
        await _context.SaveChangesAsync();
        TempData["Success"] = this.L("Thêm sách thành công", "Book added successfully");
        return RedirectToAction(nameof(Index));
    }

    [RoleAuthorize("Admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var book = await _context.Books.FindAsync(id);
        if (book == null)
        {
            return NotFound();
        }

        await LoadDropdowns();
        return View(book);
    }

    [RoleAuthorize("Admin")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Book book, IFormFile? imageFile)
    {
        if (id != book.Id)
        {
            return BadRequest();
        }

        book.BookCode = book.BookCode?.Trim() ?? string.Empty;
        book.BookName = book.BookName?.Trim() ?? string.Empty;

        var existing = await _context.Books.FindAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await LoadDropdowns();
            return View(book);
        }

        existing.BookCode = book.BookCode;
        existing.BookName = book.BookName;
        existing.CategoryId = book.CategoryId;
        existing.AuthorId = book.AuthorId;
        existing.PublisherId = book.PublisherId;
        existing.PublishYear = book.PublishYear;
        existing.RentalFee = book.RentalFee;
        existing.BookPrice = book.BookPrice;

        var borrowed = existing.Quantity - existing.AvailableQuantity;
        existing.Quantity = book.Quantity;
        existing.AvailableQuantity = Math.Max(0, book.Quantity - borrowed);
        existing.Description = book.Description;
        if (imageFile != null)
        {
            existing.ImageUrl = await SaveBookImage(imageFile);
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = this.L("Cập nhật sách thành công", "Book updated successfully");
        return RedirectToAction(nameof(Index));
    }

    [RoleAuthorize("Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var book = await _context.Books.Include(x => x.Category).Include(x => x.Author).FirstOrDefaultAsync(x => x.Id == id);
        return book == null ? NotFound() : View(book);
    }

    [RoleAuthorize("Admin")]
    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var book = await _context.Books.FindAsync(id);
        if (book == null)
        {
            return NotFound();
        }

        if (await _context.BorrowTicketDetails.AnyAsync(x => x.BookId == id))
        {
            TempData["Error"] = this.L("Không thể xóa sách đã phát sinh phiếu mượn", "Cannot delete a book that has borrow records");
            return RedirectToAction(nameof(Index));
        }

        _context.Books.Remove(book);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadDropdowns()
    {
        ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "CategoryName");
        ViewBag.Authors = new SelectList(await _context.Authors.ToListAsync(), "Id", "AuthorName");
        ViewBag.Publishers = new SelectList(await _context.Publishers.ToListAsync(), "Id", "PublisherName");
    }

    private async Task<string> SaveBookImage(IFormFile file)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
        {
            throw new InvalidOperationException(this.L("Định dạng ảnh không hợp lệ", "Invalid image format"));
        }

        var maxSize = _configuration.GetValue<int>("AppSettings:MaxUploadSizeMb") * 1024 * 1024;
        if (file.Length > maxSize)
        {
            throw new InvalidOperationException(this.L("Dung lượng ảnh vượt quá giới hạn", "Image size exceeds limit"));
        }

        var folder = Path.Combine(_environment.WebRootPath, "images", "books");
        Directory.CreateDirectory(folder);
        var fileName = $"{Guid.NewGuid():N}{ext}";
        var path = Path.Combine(folder, fileName);

        await using var stream = System.IO.File.Create(path);
        await file.CopyToAsync(stream);
        return $"/images/books/{fileName}";
    }
}
