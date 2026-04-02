using FinancialCalc.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace FinancialCalc.WebUI.ViewModels.Products.Bop
{
    public class ProductBopLineViewModel
    {
        public Guid? Id { get; set; } // пригодится на Edit

        [Range(1, int.MaxValue)]
        public int Sequence { get; set; }

        [Required]
        public OperationType Operation { get; set; }

        [Required]
        public double Duration { get; set; }

        [Display(Name = "Станок")]
        public Guid WorkstationId { get; set; }

        [Display(Name = "Должность")]
        public Guid JobPositionId { get; set; }

        // Списки для выпадающих меню
        public IEnumerable<SelectListItem>? Workstations { get; set; }
        public IEnumerable<SelectListItem>? JobPositions { get; set; }
    }
}
