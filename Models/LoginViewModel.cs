using System.ComponentModel.DataAnnotations;

namespace Notifico.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "E-posta zorunludur!")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz!")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Şifre zorunludur!")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
