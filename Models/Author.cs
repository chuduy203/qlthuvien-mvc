using System.ComponentModel.DataAnnotations;
namespace LibraryManagement.Models;
public class Author
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Tên tác giả không được để trống"), StringLength(100)] public string AuthorName { get; set; } = string.Empty;
    public string? Biography { get; set; }
    public ICollection<Book> Books { get; set; } = new List<Book>();
}
