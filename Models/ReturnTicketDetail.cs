using System.ComponentModel.DataAnnotations;
namespace LibraryManagement.Models;
public class ReturnTicketDetail
{
    public int Id { get; set; }
    public int ReturnTicketId { get; set; }
    public int BookId { get; set; }

    [Range(1, 100)] public int Quantity { get; set; }

    [Range(0, 100)] public int NormalQuantity { get; set; }

    [Range(0, 100)] public int DamagedQuantity { get; set; }

    [Range(0, 100)] public int LostQuantity { get; set; }

    public bool IsDamaged { get; set; }
    public ReturnTicket? ReturnTicket { get; set; }
    public Book? Book { get; set; }
}
