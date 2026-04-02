using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace FinanzRechner.Domain.Entities
{
    public class Order
    {
        public Guid Id { get; set; }

        public string OrderNumber { get; set; } = null!;   // внутренний номер/код заказа

        public Guid ClientId { get; set; }
        [ValidateNever]
        public Client Client { get; set; } = null!;

        public ICollection<OrderProduct> OrderProducts { get; set; } = new List<OrderProduct>();
    }
}
