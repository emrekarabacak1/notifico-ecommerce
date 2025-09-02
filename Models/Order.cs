using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Notifico.Models
{
    public class Order
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public AppUser User { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public OrderStatus Status { get; set; }
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

        public int? AddressId { get; set; }                
        public Address Address { get; set; }               

        [MaxLength(512)]
        public string AddressSnapshot { get; set; }        

    }
}
