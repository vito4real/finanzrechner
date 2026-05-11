namespace FinanzRechner.WebUI.ViewModels.Material
{
    public class MaterialDetailsViewModel
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = null!;

        public decimal UnitPrice { get; set; }

        public List<MaterialUsageViewModel> UsedInProducts { get; set; } = new();
    }
}
