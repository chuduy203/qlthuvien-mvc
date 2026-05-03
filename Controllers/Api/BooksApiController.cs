using LibraryManagement.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Controllers.Api;
[ApiController]
[Route("api/books")]
public class BooksApiController : ControllerBase
{
    private readonly LibraryDbContext _context;
    public BooksApiController(LibraryDbContext context) => _context = context;
    [HttpGet]
    public async Task<IActionResult> GetBooks(string? keyword)
    {
        var query = _context.Books.Include(x => x.Category).Include(x => x.Author).AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(x => x.BookName.Contains(keyword) || x.BookCode.Contains(keyword));
        var data = await query.Select(x => new
        {
            x.Id,
            x.BookCode,
            x.BookName,
            CategoryName = x.Category!.CategoryName,
            AuthorName = x.Author!.AuthorName,
            x.AvailableQuantity,
            x.RentalFee,
            x.BookPrice
        }).Take(20).ToListAsync();
        return Ok(data);
    }
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetBookById(int id){ var book = await _context.Books.Include(x => x.Category).Include(x => x.Author).FirstOrDefaultAsync(x => x.Id == id); return book == null ? NotFound() : Ok(book); }
}
