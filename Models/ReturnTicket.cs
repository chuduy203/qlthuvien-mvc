using System.ComponentModel.DataAnnotations;
namespace LibraryManagement.Models;
public class ReturnTicket
{
    public int Id { get; set; }
    [Required, StringLength(50)] public string ReturnCode { get; set; } = string.Empty;
    public int BorrowTicketId { get; set; }
    public DateTime ReturnDate { get; set; } = DateTime.Now;
    public int UserId { get; set; }
    public BorrowTicket? BorrowTicket { get; set; }
    public User? User { get; set; }
    public ICollection<ReturnTicketDetail> ReturnTicketDetails { get; set; } = new List<ReturnTicketDetail>();
}
