using System.ComponentModel.DataAnnotations;
namespace LibraryManagement.Models;
public class Publisher
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Tên nhà xuất bản không được để trống"), StringLength(150)] public string PublisherName { get; set; } = string.Empty;
    [StringLength(255)] public string? Address { get; set; }
    [Phone, StringLength(20)] public string? Phone { get; set; }
    public ICollection<Book> Books { get; set; } = new List<Book>();
}
