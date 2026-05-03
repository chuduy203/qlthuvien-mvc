using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.Models;

public class Reader
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Mã độc giả không được để trống"), StringLength(50)]
    public string ReaderCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Họ tên không được để trống"), StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Email không hợp lệ"), StringLength(100)]
    public string? Email { get; set; }

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ"), StringLength(20)]
    public string? Phone { get; set; }

    [StringLength(255)]
    public string? Address { get; set; }

    public DateTime? DateOfBirth { get; set; }

    [StringLength(20)]
    public string? IdentityNumber { get; set; }

    [StringLength(10)]
    public string? Gender { get; set; }

    [StringLength(100)]
    public string? Occupation { get; set; }

    [StringLength(150)]
    public string? Workplace { get; set; }

    [StringLength(30)]
    public string? MembershipType { get; set; } = "Standard";

    public DateTime? CardIssuedDate { get; set; }

    public DateTime? CardExpiryDate { get; set; }

    [StringLength(100)]
    public string? EmergencyContactName { get; set; }

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ"), StringLength(20)]
    public string? EmergencyContactPhone { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ICollection<BorrowTicket> BorrowTickets { get; set; } = new List<BorrowTicket>();
    public ICollection<Fine> Fines { get; set; } = new List<Fine>();
}
