using System.ComponentModel.DataAnnotations;
namespace LibraryManagement.Models;
public class Category
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Tên thể loại không được để trống"), StringLength(100)] public string CategoryName { get; set; } = string.Empty;
    [StringLength(500)] public string? Description { get; set; }
    public ICollection<Book> Books { get; set; } = new List<Book>();
}
