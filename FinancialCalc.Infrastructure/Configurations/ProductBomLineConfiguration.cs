using FinancialCalc.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinancialCalc.Infrastructure.Configurations;

public class ProductBomLineConfiguration : IEntityTypeConfiguration<ProductBomLine>
{
    public void Configure(EntityTypeBuilder<ProductBomLine> b)
    {
        b.ToTable("ProductBomLines");

        b.HasKey(x => x.Id);

        b.Property(x => x.Quantity)
            .IsRequired();

        // Parent product
        b.HasOne(x => x.ParentProduct)
            .WithMany(p => p.BomChildren)
            .HasForeignKey(x => x.ParentProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // Child product
        b.HasOne(x => x.ChildProduct)
            .WithMany(p => p.BomParents)
            .HasForeignKey(x => x.ChildProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // Одна и та же связь parent->child не должна повторяться
        b.HasIndex(x => new { x.ParentProductId, x.ChildProductId })
            .IsUnique();
    }
}
