using System.ComponentModel.DataAnnotations;

namespace FinanzRechner.Domain.Entities
{
    public class Product
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [StringLength(50)]
        public required string Designation { get; set; }

        [Required]
        [StringLength(50)]
        public required string Name { get; set; }

        // BOM Relationships

        public ICollection<ProductBomLine> BomChildren { get; set; } = new List<ProductBomLine>();

        public ICollection<ProductBomLine> BomParents { get; set; } = new List<ProductBomLine>();

        // BOP Relationships
        public ICollection<ProductBopLine> BopLines { get; set; } = new List<ProductBopLine>();

        // Order Relationships
        public ICollection<OrderProduct> OrderProducts { get; set; } = new List<OrderProduct>();

        // Material Relationships
        public ICollection<ProductMaterial> ProductMaterials { get; set; } = new List<ProductMaterial>();
    }
}
