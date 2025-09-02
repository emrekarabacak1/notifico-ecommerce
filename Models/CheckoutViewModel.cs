using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Notifico.Models;

namespace Notifico.ViewModels
{
    public class CheckoutViewModel
    {
        public List<Address> Addresses { get; set; }
        [Required(ErrorMessage = "Lütfen adres seçiniz.")]
        public int? SelectedAddressId { get; set; }
        public List<CartItem> CartItems { get; set; }
    }
}
