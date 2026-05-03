using System.ComponentModel.DataAnnotations;
namespace LibraryManagement.ViewModels;
public class BookViewModel
{
    public int Id { get; set; }
    [Required, StringLength(50)] public string BookCode { get; set; } = string.Empty;
    [Required, StringLength(200)] public string BookName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public int AuthorId { get; set; }
    public int PublisherId { get; set; }
    public int? PublishYear { get; set; }
    [Range(0, int.MaxValue)] public int Quantity { get; set; }
    public string? Description { get; set; }
    public IFormFile? ImageFile { get; set; }
}
