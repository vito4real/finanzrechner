using FinanzRechner.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanzRechner.Infrastructure.Configurations;

public class ProductMaterialConfiguration : IEntityTypeConfiguration<ProductMaterial>
{
    public void Configure(EntityTypeBuilder<ProductMaterial> b)
    {
        b.ToTable("ProductMaterials");

        b.HasKey(x => x.Id);

        b.Property(x => x.Quantity)
            .IsRequired()
            .HasPrecision(18, 4);

        b.HasOne(x => x.Product)
            .WithMany(p => p.ProductMaterials)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.Material)
            .WithMany(m => m.ProductMaterials)
            .HasForeignKey(x => x.MaterialId)
            .OnDelete(DeleteBehavior.Restrict);

        // чтобы один и тот же материал для продукта не повторялся
        b.HasIndex(x => new { x.ProductId, x.MaterialId })
            .IsUnique();
    }
}
