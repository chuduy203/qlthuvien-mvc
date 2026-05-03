using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.Models;

public class BorrowTicket
{
    public int Id { get; set; }

    [Required, StringLength(50)]
    public string BorrowCode { get; set; } = string.Empty;

    public int ReaderId { get; set; }
    public int UserId { get; set; }

    public DateTime BorrowDate { get; set; } = DateTime.Now;
    public DateTime DueDate { get; set; }

    [Range(typeof(decimal), "0", "9999999999999")]
    public decimal TotalRentalFee { get; set; } = 0;

    [Required, StringLength(50)]
    public string Status { get; set; } = "Borrowing";

    public Reader? Reader { get; set; }
    public User? User { get; set; }
    public ReturnTicket? ReturnTicket { get; set; }
    public ICollection<BorrowTicketDetail> BorrowTicketDetails { get; set; } = new List<BorrowTicketDetail>();
}
