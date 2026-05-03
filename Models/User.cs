using System.ComponentModel.DataAnnotations;
namespace LibraryManagement.Models;
public class User
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Họ tên không được để trống"), StringLength(100)] public string FullName { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(100)] public string Email { get; set; } = string.Empty;
    [Required, StringLength(255)] public string PasswordHash { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public Role? Role { get; set; }
}
