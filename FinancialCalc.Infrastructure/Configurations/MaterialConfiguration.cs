using FinancialCalc.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinancialCalc.Infrastructure.Configurations;

public class MaterialConfiguration : IEntityTypeConfiguration<Material>
{
    public void Configure(EntityTypeBuilder<Material> b)
    {
        b.ToTable("Materials");

        b.HasKey(x => x.Id);

        b.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        // справочник материалов — обычно уникальный
        b.HasIndex(x => x.Name)
            .IsUnique();

        // цена за единицу
        b.Property(x => x.UnitPrice)
            .HasPrecision(18, 4);

        b.HasMany(x => x.ProductMaterials)
            .WithOne(pm => pm.Material)
            .HasForeignKey(pm => pm.MaterialId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
