using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace FinanzRechner.Domain.Entities
{
    public class Client
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [StringLength(100)]
        public required string Name { get; set; }

        // 1 Client -> many Orders
        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
