using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace LibraryManagement.Models;
public class Fine
{
    public int Id { get; set; }
    public int ReaderId { get; set; }
    public int BorrowTicketId { get; set; }
    [Range(0, int.MaxValue)] public int LateDays { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal Amount { get; set; }
    [StringLength(255)] public string? Reason { get; set; }
    public bool IsPaid { get; set; }
    public Reader? Reader { get; set; }
    public BorrowTicket? BorrowTicket { get; set; }
}
