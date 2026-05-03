using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.ViewModels;

public class BorrowTicketBookItemViewModel
{
    [Required(ErrorMessage = "Vui lòng chọn sách")]
    public int BookId { get; set; }

    [Range(1, 100, ErrorMessage = "Số lượng mượn phải từ 1 đến 100")]
    public int Quantity { get; set; } = 1;
}
