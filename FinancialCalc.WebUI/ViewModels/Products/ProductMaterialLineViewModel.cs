using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace FinancialCalc.WebUI.ViewModels.Products
{
    public class ProductMaterialLineViewModel
    {
        [Required]
        public Guid MaterialId { get; set; }

        [Required]
        [Range(0.0001, double.MaxValue, ErrorMessage = "количество должно быть больше нуля")]
        public decimal Quantity { get; set; }

        [ValidateNever]
        public string? MaterialName { get; set; }
    }
}
