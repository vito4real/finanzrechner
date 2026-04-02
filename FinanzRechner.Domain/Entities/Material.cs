using System.ComponentModel.DataAnnotations;

namespace FinanzRechner.Domain.Entities
{
    public class Material
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [StringLength(100)]
        public required string Name { get; set; }

        // Цена за единицу (кг, м² и т.д.)
        [Required]
        public decimal UnitPrice { get; set; }

        // Навигация many-to-many через join
        public ICollection<ProductMaterial> ProductMaterials { get; set; } = new List<ProductMaterial>();
    }
}
