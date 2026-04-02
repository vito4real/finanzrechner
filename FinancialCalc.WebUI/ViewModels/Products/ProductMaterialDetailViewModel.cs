namespace FinancialCalc.WebUI.ViewModels.Products
{
    public class ProductMaterialDetailViewModel
    {
        public Guid Id { get; set; }

        public string MaterialName { get; set; }

        public decimal Quantity{ get; set; }

        public decimal UnitPrice { get; set; }

        public decimal TotalCost => Quantity * UnitPrice;

    }
}
