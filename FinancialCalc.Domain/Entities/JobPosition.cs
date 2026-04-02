using FinancialCalc.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace FinancialCalc.Domain.Entities
{
    public class JobPosition
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; } = null!;

        [Required]
        public decimal BaseHourlyRate { get; set; }

        [Required]
        public SeverityCategory Severity { get; set; }

        public decimal SeverityBonus
        {
            get
            {
                return Severity switch
                {
                    SeverityCategory.Ia => 1.0m,
                    SeverityCategory.Ib => 1.05m,
                    SeverityCategory.IIa => 1.12m,
                    SeverityCategory.IIb => 1.20m,
                    SeverityCategory.III => 1.35m,
                    _ => 1.0m
                };
            }
        }

        public decimal FinalHourlyRate => BaseHourlyRate * SeverityBonus;

        public ICollection<ProductBopLine> BopLines { get; set; }=new List<ProductBopLine>();
    }
}