using LibraryManagement.Data;
using LibraryManagement.Extensions;
using LibraryManagement.Filters;
using LibraryManagement.Models;
using LibraryManagement.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Controllers;

[RoleAuthorize("Admin")]
public class EmployeesController : Controller
{
    private readonly LibraryDbContext _context;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public EmployeesController(LibraryDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? keyword, int? roleId, bool? isActive)
    {
        var roles = await GetEmployeeRolesAsync();
        ViewBag.Roles = new SelectList(roles, "Id", "RoleName", roleId);
        ViewBag.Keyword = keyword;
        ViewBag.RoleId = roleId;
        ViewBag.IsActive = isActive;

        var roleIds = roles.Select(x => x.Id).ToList();
        var query = _context.Users.Include(x => x.Role).Where(x => roleIds.Contains(x.RoleId));

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(x => x.FullName.Contains(keyword) || x.Email.Contains(keyword));
        }

        if (roleId.HasValue)
        {
            query = query.Where(x => x.RoleId == roleId.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(x => x.IsActive == isActive.Value);
        }

        var data = await query.OrderByDescending(x => x.Id).ToListAsync();
        return View(data);
    }

    public async Task<IActionResult> Create()
    {
        await LoadRolesAsync();
        return View(new EmployeeFormViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EmployeeFormViewModel model)
    {
        await ValidateRoleAsync(model.RoleId);

        if (string.IsNullOrWhiteSpace(model.Password))
        {
            ModelState.AddModelError(nameof(model.Password), this.L("Mật khẩu không được để trống.", "Password is required."));
        }

        if (await _context.Users.AnyAsync(x => x.Email == model.Email))
        {
            ModelState.AddModelError(nameof(model.Email), this.L("Email đã tồn tại.", "Email already exists."));
        }

        if (!ModelState.IsValid)
        {
            await LoadRolesAsync(model.RoleId);
            return View(model);
        }

        var user = new User
        {
            FullName = model.FullName.Trim(),
            Email = model.Email.Trim(),
            RoleId = model.RoleId,
            IsActive = model.IsActive,
            CreatedAt = DateTime.Now
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, model.Password!);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        TempData["Success"] = this.L("Đã thêm nhân viên thành công.", "Employee added successfully.");
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var user = await FindEmployeeByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        await LoadRolesAsync(user.RoleId);
        return View(new EmployeeFormViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            RoleId = user.RoleId,
            IsActive = user.IsActive
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EmployeeFormViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        await ValidateRoleAsync(model.RoleId);
        var user = await FindEmployeeByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        if (await _context.Users.AnyAsync(x => x.Email == model.Email && x.Id != model.Id))
        {
            ModelState.AddModelError(nameof(model.Email), this.L("Email đã tồn tại.", "Email already exists."));
        }

        if (!string.IsNullOrWhiteSpace(model.Password) && model.Password.Length < 6)
        {
            ModelState.AddModelError(nameof(model.Password), this.L("Mật khẩu tối thiểu 6 ký tự.", "Password must be at least 6 characters."));
        }

        if (!ModelState.IsValid)
        {
            await LoadRolesAsync(model.RoleId);
            return View(model);
        }

        user.FullName = model.FullName.Trim();
        user.Email = model.Email.Trim();
        user.RoleId = model.RoleId;
        user.IsActive = model.IsActive;
        if (!string.IsNullOrWhiteSpace(model.Password))
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = this.L("Đã cập nhật nhân viên thành công.", "Employee updated successfully.");
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var user = await FindEmployeeByIdAsync(id);
        return user == null ? NotFound() : View(user);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var user = await FindEmployeeByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var currentUserId = HttpContext.Session.GetInt32("UserId");
        if (currentUserId == id)
        {
            TempData["Error"] = this.L("Không thể xóa chính tài khoản đang đăng nhập.", "You cannot delete the currently signed-in account.");
            return RedirectToAction(nameof(Index));
        }

        var hasRelatedData = await _context.BorrowTickets.AnyAsync(x => x.UserId == id) ||
                             await _context.ReturnTickets.AnyAsync(x => x.UserId == id);

        if (hasRelatedData)
        {
            user.IsActive = false;
            await _context.SaveChangesAsync();
            TempData["Success"] = this.L(
                "Nhân viên đã có dữ liệu phát sinh, hệ thống đã khóa tài khoản thay vì xóa.",
                "This employee has related records, so the account was locked instead of deleted."
            );
            return RedirectToAction(nameof(Index));
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        TempData["Success"] = this.L("Đã xóa nhân viên thành công.", "Employee deleted successfully.");
        return RedirectToAction(nameof(Index));
    }

    private async Task<User?> FindEmployeeByIdAsync(int id)
    {
        var roles = await GetEmployeeRolesAsync();
        var roleIds = roles.Select(x => x.Id).ToList();
        return await _context.Users.Include(x => x.Role).FirstOrDefaultAsync(x => x.Id == id && roleIds.Contains(x.RoleId));
    }

    private async Task<List<Role>> GetEmployeeRolesAsync()
    {
        return await _context.Roles
            .Where(x => x.RoleName == "Admin" || x.RoleName == "Librarian")
            .OrderBy(x => x.Id)
            .ToListAsync();
    }

    private async Task ValidateRoleAsync(int roleId)
    {
        var isEmployeeRole = await _context.Roles.AnyAsync(x => x.Id == roleId && (x.RoleName == "Admin" || x.RoleName == "Librarian"));
        if (!isEmployeeRole)
        {
            ModelState.AddModelError(nameof(EmployeeFormViewModel.RoleId), this.L("Vai trò không hợp lệ.", "Invalid role."));
        }
    }

    private async Task LoadRolesAsync(int? roleId = null)
    {
        var roles = await GetEmployeeRolesAsync();
        ViewBag.Roles = new SelectList(roles, "Id", "RoleName", roleId);
    }
}
