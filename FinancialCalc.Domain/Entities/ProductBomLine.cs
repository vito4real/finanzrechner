using System.ComponentModel.DataAnnotations;

namespace FinancialCalc.Domain.Entities
{
    public class ProductBomLine
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid ParentProductId { get; set; }
        public Product ParentProduct { get; set; } = null!;

        [Required]
        public Guid ChildProductId { get; set; }
        public Product ChildProduct { get; set; } = null!;

        [Required]
        public int Quantity { get; set; }
    }
}
