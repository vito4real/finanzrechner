using System.ComponentModel.DataAnnotations;

namespace FinanzRechner.WebUI.ViewModels
{
    public class OrderProductLineViewModel
    {
        [Required(ErrorMessage="Выберите продукт")]
        public Guid? ProductId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Количество должно быть не менее 1")]
        public int Quantity { get; set; }

        public string? ProductName { get; set; }
    }
}
