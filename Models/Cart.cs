using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Notifico.Models
{
    public class Cart
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public AppUser User { get; set; }
        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    }
}
