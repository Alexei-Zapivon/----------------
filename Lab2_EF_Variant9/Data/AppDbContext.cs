using Lab2_EF_Variant9.Models;
using Microsoft.EntityFrameworkCore;

namespace Lab2_EF_Variant9.Data;

public class AppDbContext : DbContext
{
    // Единственная таблица — TPH хранит все типы в ней
    public DbSet<Document> Documents { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite("Data Source=lab2.db");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Document>(e =>
        {
            e.HasKey(d => d.Id);
            e.Property(d => d.Id).ValueGeneratedOnAdd();
            e.Property(d => d.Title).IsRequired().HasMaxLength(200);
            e.Property(d => d.Description).HasMaxLength(500);

            // TPH: EF Core добавит колонку "DocumentType" как дискриминатор
            e.HasDiscriminator<string>("DocumentType")
                .HasValue<Receipt>("Receipt")
                .HasValue<Invoice>("Invoice")
                .HasValue<Bill>("Bill");
        });

        modelBuilder.Entity<Receipt>(e =>
        {
            e.Property(r => r.PayerName).HasMaxLength(100);
            e.Property(r => r.ReceiverName).HasMaxLength(100);
            e.Property(r => r.PaymentMethod).HasMaxLength(50);
        });

        modelBuilder.Entity<Invoice>(e =>
        {
            e.Property(i => i.SupplierName).HasMaxLength(100);
            e.Property(i => i.BuyerName).HasMaxLength(100);
            e.Property(i => i.ShippingAddress).HasMaxLength(200);
        });

        modelBuilder.Entity<Bill>(e =>
        {
            e.Property(b => b.CustomerName).HasMaxLength(100);
            e.Property(b => b.BankAccount).HasMaxLength(50);
        });
    }
}
