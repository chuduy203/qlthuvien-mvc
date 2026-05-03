using LibraryManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Data;

public class LibraryDbContext : DbContext
{
    public LibraryDbContext(DbContextOptions<LibraryDbContext> options) : base(options) { }
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Author> Authors => Set<Author>();
    public DbSet<Publisher> Publishers => Set<Publisher>();
    public DbSet<Book> Books => Set<Book>();
    public DbSet<Reader> Readers => Set<Reader>();
    public DbSet<BorrowTicket> BorrowTickets => Set<BorrowTicket>();
    public DbSet<BorrowTicketDetail> BorrowTicketDetails => Set<BorrowTicketDetail>();
    public DbSet<ReturnTicket> ReturnTickets => Set<ReturnTicket>();
    public DbSet<ReturnTicketDetail> ReturnTicketDetails => Set<ReturnTicketDetail>();
    public DbSet<Fine> Fines => Set<Fine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Role>().HasIndex(x => x.RoleName).IsUnique();
        modelBuilder.Entity<User>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<Book>().HasIndex(x => x.BookCode).IsUnique();
        modelBuilder.Entity<Reader>().HasIndex(x => x.ReaderCode).IsUnique();
        modelBuilder.Entity<Reader>().HasIndex(x => x.IdentityNumber).IsUnique().HasFilter("[IdentityNumber] IS NOT NULL");
        modelBuilder.Entity<BorrowTicket>().HasIndex(x => x.BorrowCode).IsUnique();
        modelBuilder.Entity<ReturnTicket>().HasIndex(x => x.ReturnCode).IsUnique();

        modelBuilder.Entity<Book>().Property(x => x.RentalFee).HasPrecision(18, 2);
        modelBuilder.Entity<Book>().Property(x => x.BookPrice).HasPrecision(18, 2);
        modelBuilder.Entity<BorrowTicket>().Property(x => x.TotalRentalFee).HasPrecision(18, 2);
        modelBuilder.Entity<BorrowTicketDetail>().Property(x => x.UnitRentalFee).HasPrecision(18, 2);
        modelBuilder.Entity<BorrowTicketDetail>().Property(x => x.LineRentalFee).HasPrecision(18, 2);

        modelBuilder.Entity<ReturnTicket>()
            .HasOne(x => x.BorrowTicket)
            .WithOne(x => x.ReturnTicket)
            .HasForeignKey<ReturnTicket>(x => x.BorrowTicketId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Role>().HasData(
            new Role { Id = 1, RoleName = "Admin" },
            new Role { Id = 2, RoleName = "Librarian" },
            new Role { Id = 3, RoleName = "Reader" }
        );
    }
}
