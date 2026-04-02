using System.ComponentModel.DataAnnotations;

namespace FinancialCalc.WebUI.ViewModels.Products.Bom
{
    public class ProductBomLineViewModel
    {
        public Guid? Id { get; set; }   // Id строки BOM (ProductBomLine.Id)

        [Required]
        public Guid? ChildProductId { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; } = 1;
    }
}