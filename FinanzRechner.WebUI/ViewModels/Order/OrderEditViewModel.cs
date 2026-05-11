using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace FinanzRechner.WebUI.ViewModels.Order
{
    public class OrderEditViewModel
    {
        public Guid? Id { get; set; }

        [Required]
        [Display(Name ="Номер заказа")]
        public string OrderNumber { get; set; } = null!;

        [Required]
        [Display(Name ="Клиент")]
        public Guid ClientId { get; set; }

        [Display(Name ="Дата заказа")]
        public DateTime OrderDate { get; set; }= DateTime.Now;

        public List<OrderProductLineViewModel> Items { get; set; } = new();

        [ValidateNever]
        public SelectList? ClientsSelectList { get; set; }

        public SelectList? ProductSelectList { get; set; }
    }
}
