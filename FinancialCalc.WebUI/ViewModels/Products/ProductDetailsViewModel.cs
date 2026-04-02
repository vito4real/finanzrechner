using FinancialCalc.WebUI.ViewModels.Products.Bom;
using FinancialCalc.WebUI.ViewModels.Products.Bop;

namespace FinancialCalc.WebUI.ViewModels.Products
{
    public class ProductDetailsViewModel
    {
        public Guid Id { get; set; }
        public string Designation { get; set; } = null!;
        public string Name { get; set; } = null!;

        // дерево состава изделия (BOM)
        public List<ProductBomTreeViewModel> BomTree { get; set; } = new();

        // техпроцесс изделия (BOP)
        public List<ProductBopRouteViewModel> BopLines { get; set; } = new();

        // где используется изделие
        public List<ProductWhereUsedViewModel> WhereUsed { get; set; } = new();

        public List<ProductMaterialDetailViewModel> Materials { get; set; } = new();
    }
}
