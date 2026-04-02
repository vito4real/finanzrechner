using System.ComponentModel.DataAnnotations;

namespace FinanzRechner.Domain.Entities
{
    public class Workstation
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = null!; // Puma 400XL MB

        [Required]
        [StringLength(50)]
        public string Code { get; set; } = null!;   // 27

        // Вычисляемое свойство (в БД не сохраняется)
        public string DisplayName => $"{Code} - {Name}";

        [StringLength(250)]
        public string? Description { get; set; }

        // Электроэнергия: кВт⋅ч/час и цена за кВт⋅ч
        public decimal EnergyKwhPerHour { get; set; }
        public decimal EnergyPricePerKwh { get; set; }

        // Охлаждающая жидкость: литры/час и цена за литр
        public decimal CoolantLitersPerHour { get; set; }
        public decimal CoolantPricePerLiter { get; set; }

        // Навигация 1 станок -> много BOP линий
        public ICollection<ProductBopLine> BopLines { get; set; } = new List<ProductBopLine>();

        public decimal MachineHourlyCost =>
            (EnergyKwhPerHour * EnergyPricePerKwh) +
            (CoolantLitersPerHour * CoolantPricePerLiter);

    }
}
