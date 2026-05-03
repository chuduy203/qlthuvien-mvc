using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.Models;

public class Book
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Mã sách không được để trống"), StringLength(50)]
    public string BookCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tên sách không được để trống"), StringLength(200)]
    public string BookName { get; set; } = string.Empty;

    [Required]
    public int CategoryId { get; set; }

    [Required]
    public int AuthorId { get; set; }

    [Required]
    public int PublisherId { get; set; }

    [Range(1900, 2100, ErrorMessage = "Năm xuất bản không hợp lệ")]
    public int? PublishYear { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Số lượng không hợp lệ")]
    public int Quantity { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Số lượng còn lại không hợp lệ")]
    public int AvailableQuantity { get; set; }

    [Range(typeof(decimal), "0", "9999999999999")]
    public decimal RentalFee { get; set; } = 0;

    [Range(typeof(decimal), "0", "9999999999999")]
    public decimal BookPrice { get; set; } = 0;

    [StringLength(255)]
    public string? ImageUrl { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public Category? Category { get; set; }
    public Author? Author { get; set; }
    public Publisher? Publisher { get; set; }
    public ICollection<BorrowTicketDetail> BorrowTicketDetails { get; set; } = new List<BorrowTicketDetail>();
}
