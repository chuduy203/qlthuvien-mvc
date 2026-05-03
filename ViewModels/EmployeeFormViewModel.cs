using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.ViewModels;

public class EmployeeFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Họ tên không được để trống"), StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email không được để trống"), EmailAddress, StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn vai trò")]
    public int RoleId { get; set; }

    [DataType(DataType.Password), StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
    public string? Password { get; set; }

    public bool IsActive { get; set; } = true;
}
