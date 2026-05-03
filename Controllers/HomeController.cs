using LibraryManagement.Data;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Controllers;
public class HomeController : Controller
{
    private readonly LibraryDbContext _context;
    public HomeController(LibraryDbContext context) => _context = context;

    public async Task<IActionResult> Index()
    {
        ViewBag.TotalBooks = await _context.Books.CountAsync();
        ViewBag.TotalReaders = await _context.Readers.CountAsync();
        ViewBag.Borrowing = await _context.BorrowTickets.CountAsync(x => x.Status == "Borrowing");
        ViewBag.Overdue = await _context.BorrowTickets.CountAsync(x => x.Status == "Borrowing" && x.DueDate < DateTime.Now);
        var books = await _context.Books.Include(x => x.Author).Include(x => x.Category)
            .OrderByDescending(x => x.Id).Take(8).ToListAsync();
        return View(books);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SetLanguage(string culture, string returnUrl)
    {
        var supportedCultures = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "vi-VN",
            "en-US"
        };

        var selectedCulture = supportedCultures.Contains(culture) ? culture : "vi-VN";
        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(selectedCulture)),
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) });

        if (string.IsNullOrWhiteSpace(returnUrl) || !Url.IsLocalUrl(returnUrl))
        {
            return RedirectToAction(nameof(Index));
        }

        return LocalRedirect(returnUrl);
    }
}
