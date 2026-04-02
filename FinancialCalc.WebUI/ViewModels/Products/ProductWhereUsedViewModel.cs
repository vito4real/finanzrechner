namespace FinancialCalc.WebUI.ViewModels.Products
{
    public class ProductWhereUsedViewModel
    {
        public Guid ParentProductId { get; set; }
        public string ParentDesignation { get; set; } = null!;
        public string ParentName { get; set; } = null!;
        // Сколько ТЕКУЩЕГО изделия входит в родительское (на 1 шт. родителя)
        public int Quantity { get; set; } = 1;
    }
}
