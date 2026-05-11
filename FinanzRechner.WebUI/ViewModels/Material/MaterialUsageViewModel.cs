using Humanizer;

namespace FinanzRechner.WebUI.ViewModels.Material
{
    public class MaterialUsageViewModel
    {
        public Guid ProductId { get; set; }

        public string ProductDesignation { get; set; } = null!;

        public string ProductName { get; set; }=null!;

        public decimal Quantity { get; set; }
    }
}
