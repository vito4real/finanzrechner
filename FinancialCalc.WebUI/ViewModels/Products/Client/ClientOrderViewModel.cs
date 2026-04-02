namespace FinancialCalc.WebUI.ViewModels.Products.Client
{
    public class ClientOrderViewModel
    {
        public Guid OrderId { get; set; }

        public string OrderNumber { get; set; } = null!;

        public int TotalProductsCount { get; set; }
    }
}
