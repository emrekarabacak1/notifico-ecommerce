using System.ComponentModel.DataAnnotations;

namespace Notifico.Models
{
    public class ProfileViewModel
    {
        [Required(ErrorMessage ="Kullanıcı adı zorunludur!")]
        public string UserName { get; set; }

        [Required(ErrorMessage ="E-posta zorunludur")]
        [EmailAddress(ErrorMessage ="Geçerli bir e-posta adresi giriniz!")]
        public string Email { get; set; }
    }
}
