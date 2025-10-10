using Kafka.Consumer.Models;
using Microsoft.EntityFrameworkCore;

namespace Kafka.Consumer.Data;

public class CheckoutsDbContext(DbContextOptions<CheckoutsDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<PaymentMethodFee> PaymentMethodFees { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            
            entity.Property(e => e.OrderId)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(e => e.CustomerId)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(e => e.CustomerEmail)
                .IsRequired()
                .HasMaxLength(255);
            
            entity.Property(e => e.Currency)
                .IsRequired()
                .HasMaxLength(10);
            
            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(50);
            
            entity.Property(e => e.TotalAmount)
                .HasColumnType("decimal(18,2)");
            
            entity.Property(e => e.ShippingFirstName)
                .HasMaxLength(100);
            
            entity.Property(e => e.ShippingLastName)
                .HasMaxLength(100);
            
            entity.Property(e => e.ShippingStreet)
                .HasMaxLength(500);
            
            entity.Property(e => e.ShippingCity)
                .HasMaxLength(100);
            
            entity.Property(e => e.ShippingPostalCode)
                .HasMaxLength(20);
            
            entity.Property(e => e.ShippingCountry)
                .HasMaxLength(100);
            
            entity.Property(e => e.EventId)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(e => e.ItemsJson)
                .HasColumnType("jsonb");

            // Add indexes for better query performance
            entity.HasIndex(e => e.OrderId).IsUnique();
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.EventId).IsUnique();
            entity.HasIndex(e => e.OrderDate);
            entity.HasIndex(e => e.ProcessedAt);
            entity.HasIndex(e => e.Status);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            
            entity.Property(e => e.OrderId)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(e => e.PaymentMethod)
                .IsRequired()
                .HasMaxLength(50);
            
            entity.Property(e => e.PaymentProvider)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(e => e.TransactionId)
                .IsRequired()
                .HasMaxLength(200);
            
            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Amount)
                .HasColumnType("decimal(18,2)");
            
            entity.Property(e => e.FeeAmount)
                .HasColumnType("decimal(18,2)");
            
            entity.Property(e => e.FeePercentage)
                .HasColumnType("decimal(5,3)");
            
            entity.HasIndex(e => e.OrderId).IsUnique();
            entity.HasIndex(e => e.TransactionId).IsUnique();
            entity.HasIndex(e => e.ProcessedAt);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.PaymentMethod);

            // Configure the relationship
            entity.HasOne(p => p.Order)
                .WithOne(o => o.Payment)
                .HasForeignKey<Payment>(p => p.OrderId)
                .HasPrincipalKey<Order>(o => o.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PaymentMethodFee>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            
            entity.Property(e => e.PaymentMethod)
                .IsRequired()
                .HasMaxLength(50);
            
            entity.Property(e => e.FeePercentage)
                .HasColumnType("decimal(5,3)")
                .IsRequired();
            
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true);
            
            entity.Property(e => e.CreatedAt)
                .IsRequired();
            
            entity.Property(e => e.UpdatedAt)
                .IsRequired();

            // Add indexes for better query performance
            entity.HasIndex(e => e.PaymentMethod).IsUnique();
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.CreatedAt);

            // Seed data
            var now = DateTime.UtcNow;
            entity.HasData(
                new PaymentMethodFee { Id = 1, PaymentMethod = "CreditCard", FeePercentage = 2.0m, IsActive = true, CreatedAt = now, UpdatedAt = now },
                new PaymentMethodFee { Id = 2, PaymentMethod = "DebitCard", FeePercentage = 0.9m, IsActive = true, CreatedAt = now, UpdatedAt = now },
                new PaymentMethodFee { Id = 3, PaymentMethod = "PayPal", FeePercentage = 1.5m, IsActive = true, CreatedAt = now, UpdatedAt = now },
                new PaymentMethodFee { Id = 4, PaymentMethod = "ApplePay", FeePercentage = 1.2m, IsActive = true, CreatedAt = now, UpdatedAt = now },
                new PaymentMethodFee { Id = 5, PaymentMethod = "GooglePay", FeePercentage = 1.1m, IsActive = true, CreatedAt = now, UpdatedAt = now },
                new PaymentMethodFee { Id = 6, PaymentMethod = "BankTransfer", FeePercentage = 0.8m, IsActive = true, CreatedAt = now, UpdatedAt = now },
                new PaymentMethodFee { Id = 7, PaymentMethod = "Swish", FeePercentage = 0.7m, IsActive = true, CreatedAt = now, UpdatedAt = now },
                new PaymentMethodFee { Id = 8, PaymentMethod = "Unknown", FeePercentage = 1.5m, IsActive = true, CreatedAt = now, UpdatedAt = now }
            );
        });
    }
}