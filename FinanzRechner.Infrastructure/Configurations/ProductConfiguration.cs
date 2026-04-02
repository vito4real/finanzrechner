using FinanzRechner.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanzRechner.Infrastructure.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> b)
    {
        b.ToTable("Products");

        b.HasKey(x => x.Id);

        b.Property(x => x.Designation)
            .IsRequired()
            .HasMaxLength(50);

        b.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(50);

        // Designation уникален в системе
        b.HasIndex(x => x.Designation)
            .IsUnique();

        // BOM: parent -> children
        b.HasMany(x => x.BomChildren)
            .WithOne(x => x.ParentProduct)
            .HasForeignKey(x => x.ParentProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // BOM: child -> parents
        b.HasMany(x => x.BomParents)
            .WithOne(x => x.ChildProduct)
            .HasForeignKey(x => x.ChildProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // BOP
        b.HasMany(x => x.BopLines)
            .WithOne(x => x.Product)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
