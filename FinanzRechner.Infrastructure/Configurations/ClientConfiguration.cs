using FinanzRechner.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanzRechner.Infrastructure.Configurations;

public class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> b)
    {
        b.ToTable("Clients");

        b.HasKey(x => x.Id);

        b.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        b.HasIndex(x => x.Name); // не делаю unique, т.к. могут быть одинаковые названия

        b.HasMany(x => x.Orders)
            .WithOne(o => o.Client)
            .HasForeignKey(o => o.ClientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
