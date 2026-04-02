using System.ComponentModel.DataAnnotations;

namespace FinanzRechner.Domain.Entities
{
    public class ProductMaterial
    {
        [Key]
        public Guid Id { get; set; }

        // FK -> Product
        public Guid ProductId { get; set; }
        public Product Product { get; set; } = null!;

        // FK -> Material
        public Guid MaterialId { get; set; }
        public Material Material { get; set; } = null!;

        // Количество материала в продукте
        [Required]
        public decimal Quantity { get; set; }
    }
}
