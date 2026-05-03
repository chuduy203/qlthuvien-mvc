using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.ViewModels;

public class ReturnBookItemViewModel
{
    public int BookId { get; set; }

    public string BookCode { get; set; } = string.Empty;

    public string BookName { get; set; } = string.Empty;

    public int BorrowedQuantity { get; set; }

    [Range(0, 100, ErrorMessage = "Số lượng bình thường không hợp lệ.")]
    public int NormalQuantity { get; set; }

    [Range(0, 100, ErrorMessage = "Số lượng rách/hỏng không hợp lệ.")]
    public int DamagedQuantity { get; set; }

    [Range(0, 100, ErrorMessage = "Số lượng mất không hợp lệ.")]
    public int LostQuantity { get; set; }

    public decimal RentalFee { get; set; }

    public decimal BookPrice { get; set; }
}
