using FinancialCalc.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace FinancialCalc.Infrastructure.Configurations
{
    public class JobPositionConfiguration: IEntityTypeConfiguration<JobPosition>
    {
        public void Configure(EntityTypeBuilder<JobPosition> b)
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Title).IsRequired().HasMaxLength(100);
            b.Property(x => x.BaseHourlyRate).HasPrecision(18, 2);

            // Настраиваем связь 1 Должность -> Много BOP-линий
            b.HasMany(x => x.BopLines)
                   .WithOne(x => x.JobPosition)
                   .HasForeignKey(x => x.JobPositionId)
                   .OnDelete(DeleteBehavior.Restrict); // Чтобы случайно не удалить всё разом
        }
    }
}
