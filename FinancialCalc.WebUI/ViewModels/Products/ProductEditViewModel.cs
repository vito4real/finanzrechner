using FinancialCalc.Domain.Entities;
using FinancialCalc.WebUI.ViewModels.Products.Bom;
using FinancialCalc.WebUI.ViewModels.Products.Bop;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace FinancialCalc.WebUI.ViewModels.Products
{
    public class ProductEditViewModel
    {
        public Guid? Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Designation { get; set; } = null!;

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = null!;

        public List<ProductBomLineViewModel> BomLines { get; set; } = new();

        // Техпроцесс
        public List<ProductBopLineViewModel> BopLines { get; set; } = new();

        // Dropdown для enum OperationType
        [ValidateNever]
        public List<SelectListItem> OperationOptions { get; set; } = new();

        public List<ProductMaterialLineViewModel> MaterialLines { get; set; } = new();

        [ValidateNever]
        public SelectList MaterialsSelectList { get; set; } = null!;
    }
}
