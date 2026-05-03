using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.ViewModels;

public class BorrowTicketCreateViewModel
{
    [Required(ErrorMessage = "Vui lòng chọn độc giả")]
    public int ReaderId { get; set; }

    [DataType(DataType.Date)]
    public DateTime BorrowDate { get; set; } = DateTime.Today;

    [DataType(DataType.Date)]
    public DateTime DueDate { get; set; } = DateTime.Today.AddDays(14);

    public List<BorrowTicketBookItemViewModel> Items { get; set; } =
    [
        new BorrowTicketBookItemViewModel()
    ];
}
