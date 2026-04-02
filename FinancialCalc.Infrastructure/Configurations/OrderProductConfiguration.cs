using FinancialCalc.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinancialCalc.Infrastructure.Configurations;

public class OrderProductConfiguration : IEntityTypeConfiguration<OrderProduct>
{
    public void Configure(EntityTypeBuilder<OrderProduct> b)
    {
        b.ToTable("OrderProducts");

        b.HasKey(x => x.Id);

        b.Property(x => x.Quantity)
            .IsRequired();

        b.HasOne(x => x.Order)
            .WithMany(o => o.OrderProducts)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.Product)
            .WithMany(p => p.OrderProducts)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // чтобы не было дублей одного и того же продукта в рамках заказа
        b.HasIndex(x => new { x.OrderId, x.ProductId })
            .IsUnique();
    }
}
