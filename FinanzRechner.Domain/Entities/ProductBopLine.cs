using FinanzRechner.Domain.Enums;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace FinanzRechner.Domain.Entities
{
    public class ProductBopLine
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid ProductId { get; set; }
        public Product Product { get; set; } = null!;

        [Required]
        public int Sequence { get; set; }

        [Required]
        public OperationType Operation { get; set; }

        [Required]
        [Range(0.01, 10000)]
        public double Duration { get; set; }

        [ValidateNever]
        public Guid WorkstationId { get; set; }

        public Workstation Workstation { get; set; } = null!;

        [ValidateNever]
        public Guid JobPositionId { get; set; }

        public JobPosition JobPosition { get; set; }=null!;

        // public double Duration  -  как продолжительность операции (для расчета потребления ОЖ и ЭЭ)

        //WS
        public decimal TotalOperationCost
        {
            get
            {
                decimal hours = (decimal)Duration / 60m;

                decimal laborCost = hours * JobPosition.FinalHourlyRate;

                decimal mashineCost = hours * Workstation.MachineHourlyCost;

                return mashineCost + laborCost;
            }
        }
    }
}
