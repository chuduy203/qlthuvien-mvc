using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.Models;

public class BorrowTicketDetail
{
    public int Id { get; set; }
    public int BorrowTicketId { get; set; }
    public int BookId { get; set; }

    [Range(1, 100)]
    public int Quantity { get; set; }

    [Range(typeof(decimal), "0", "9999999999999")]
    public decimal UnitRentalFee { get; set; } = 0;

    [Range(typeof(decimal), "0", "9999999999999")]
    public decimal LineRentalFee { get; set; } = 0;

    public BorrowTicket? BorrowTicket { get; set; }
    public Book? Book { get; set; }
}
