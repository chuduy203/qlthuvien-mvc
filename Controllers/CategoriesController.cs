using LibraryManagement.Data;
using LibraryManagement.Filters;
using LibraryManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Controllers;
[RoleAuthorize("Admin", "Librarian")]
public class CategoriesController : Controller
{
    private readonly LibraryDbContext _context;
    public CategoriesController(LibraryDbContext context) => _context = context;
    public async Task<IActionResult> Index(string? keyword)
    {
        var query = _context.Categories.AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(x => x.CategoryName.Contains(keyword));
        ViewBag.Keyword = keyword;
        return View(await query.OrderByDescending(x => x.Id).ToListAsync());
    }
    public IActionResult Create() => View(new Category());
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Category model)
    {
        if (!ModelState.IsValid) return View(model);
        _context.Categories.Add(model); await _context.SaveChangesAsync(); return RedirectToAction(nameof(Index));
    }
    public async Task<IActionResult> Edit(int id) { var model = await _context.Categories.FindAsync(id); return model == null ? NotFound() : View(model); }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Category model)
    {
        if (id != model.Id) return BadRequest();
        if (!ModelState.IsValid) return View(model);
        _context.Update(model); await _context.SaveChangesAsync(); return RedirectToAction(nameof(Index));
    }
    public async Task<IActionResult> Delete(int id) { var model = await _context.Categories.FindAsync(id); return model == null ? NotFound() : View(model); }
    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id) { var model = await _context.Categories.FindAsync(id); if (model != null) { _context.Categories.Remove(model); await _context.SaveChangesAsync(); } return RedirectToAction(nameof(Index)); }
}
