using FinanzRechner.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanzRechner.Infrastructure.Configurations;

public class ProductBopLineConfiguration : IEntityTypeConfiguration<ProductBopLine>
{
    public void Configure(EntityTypeBuilder<ProductBopLine> b)
    {
        b.ToTable("ProductBopLines");

        b.HasKey(x => x.Id);

        b.Property(x => x.Sequence)
            .IsRequired();

        b.Property(x => x.Operation)
            .IsRequired(); // enum -> int по умолчанию

        // В рамках одного продукта Sequence уникален
        b.HasIndex(x => new { x.ProductId, x.Sequence })
            .IsUnique();

        b.HasOne(x => x.Product)
            .WithMany(p => p.BopLines)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Workstation)
            .WithMany(x => x.BopLines)
            .HasForeignKey(x => x.WorkstationId);

        b.HasOne(x => x.JobPosition)
            .WithMany(x => x.BopLines)
            .HasForeignKey(x => x.JobPositionId);
    }
}
