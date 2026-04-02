using Humanizer;

namespace FinancialCalc.WebUI.ViewModels.Products.Bom
{
    public class ProductBomTreeViewModel
    {
        public Guid ProductId { get; set; }
        public string Designation { get; set; } = null!;
        public string Name { get; set; } = null!;
        public int Quantity { get; set; }

        public decimal UnitPrice {  get; set; }

        public decimal TotalPrice => Quantity * UnitPrice;

        public List<ProductBomTreeViewModel> Children { get; set; } = new();


    }
}
