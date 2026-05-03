using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.ViewModels;

public class ReturnBookProcessViewModel
{
    [Required]
    public int BorrowTicketId { get; set; }

    public string BorrowCode { get; set; } = string.Empty;

    public string ReaderName { get; set; } = string.Empty;

    public DateTime BorrowDate { get; set; }

    public DateTime DueDate { get; set; }

    [DataType(DataType.Date)]
    public DateTime ReturnDate { get; set; } = DateTime.Today;

    public List<ReturnBookItemViewModel> Items { get; set; } = [];
}
