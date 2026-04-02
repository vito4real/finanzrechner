using FinancialCalc.Domain.Enums;

namespace FinancialCalc.WebUI.ViewModels.Products.Bop
{
    public class ProductBopRouteViewModel
    {
        public int Sequence { get; set; }
        public OperationType Operation { get; set; }

        public string WorkstationName { get; set; } = null!; 

        public string JobTitle { get; set; } = null!;

        public double Duration { get; set; }

        public decimal WorkstationRate { get; set; }

        public decimal JobRate { get; set; }

        public decimal TotalCost { get; set; }
    }

}
