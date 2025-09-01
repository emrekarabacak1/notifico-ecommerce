using System.ComponentModel.DataAnnotations;

namespace Notifico.Models
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "E-posta zorunludur!")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta giriniz!")]
        public string Email { get; set; }
    }
}