using FinanzRechner.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanzRechner.Infrastructure.Configurations;

public class WorkstationConfiguration : IEntityTypeConfiguration<Workstation>
{
    public void Configure(EntityTypeBuilder<Workstation> b)
    {
        b.ToTable("Workstations");

        b.HasKey(x => x.Id);

        b.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        b.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(50);

        // Код станка должен быть уникальным
        b.HasIndex(x => x.Code)
            .IsUnique();

        b.Property(x => x.Description)
            .HasMaxLength(75);

        // Финансовые параметры
        b.HasMany(x => x.BopLines)
            .WithOne(x => x.Workstation)
            .HasForeignKey(x => x.WorkstationId)
            .OnDelete(DeleteBehavior.Restrict);
        b.Property(x => x.EnergyKwhPerHour)
            .HasPrecision(18, 4);

        b.Property(x => x.EnergyPricePerKwh)
            .HasPrecision(18, 4);

        b.Property(x => x.CoolantLitersPerHour)
            .HasPrecision(18, 4);

        b.Property(x => x.CoolantPricePerLiter)
            .HasPrecision(18, 4);
    }
}
