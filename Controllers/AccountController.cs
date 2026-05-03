using System.Security.Claims;
using LibraryManagement.Data;
using LibraryManagement.Extensions;
using LibraryManagement.Models;
using LibraryManagement.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Controllers;
public class AccountController : Controller
{
    private readonly LibraryDbContext _context;
    private readonly PasswordHasher<User> _passwordHasher = new();
    public AccountController(LibraryDbContext context) => _context = context;

    [AllowAnonymous]
    public IActionResult Login() => View(new LoginViewModel { Email = Request.Cookies["LastEmail"] ?? string.Empty });

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await _context.Users.Include(x => x.Role).FirstOrDefaultAsync(x => x.Email == model.Email && x.IsActive);
        if (user == null || !IsPasswordValid(user, model.Password))
        {
            ModelState.AddModelError(string.Empty, this.L("Email hoặc mật khẩu không đúng", "Invalid email or password"));
            return View(model);
        }
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role?.RoleName ?? "Reader")
        };
        var identity = new ClaimsIdentity(claims, "LibraryCookie");
        await HttpContext.SignInAsync("LibraryCookie", new ClaimsPrincipal(identity), new AuthenticationProperties
        {
            IsPersistent = model.RememberMe,
            ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(7) : null
        });
        HttpContext.Session.SetInt32("UserId", user.Id);
        HttpContext.Session.SetString("FullName", user.FullName);
        HttpContext.Session.SetString("Role", user.Role?.RoleName ?? "Reader");
        if (model.RememberMe) Response.Cookies.Append("LastEmail", user.Email, new CookieOptions { Expires = DateTimeOffset.Now.AddDays(30), HttpOnly = true });
        return RedirectToAction("Index", "Home");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        HttpContext.Session.Clear();
        await HttpContext.SignOutAsync("LibraryCookie");
        return RedirectToAction("Login");
    }

    public IActionResult AccessDenied() => View();

    private bool IsPasswordValid(User user, string providedPassword)
    {
        try
        {
            return _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, providedPassword) != PasswordVerificationResult.Failed;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
