namespace FinanzRechner.WebUI.ViewModels.Order
{
    public class OrderDetailsViewModel
    {
        public Guid Id { get; set; }

        public string OrderNumber { get; set; } = null!;

        public string ClientName { get; set; } = null!;

        public decimal TotalOrderCost { get; set; }

        public List<OrderProductItemViewModel> Products { get; set; } = new();
    }
}
