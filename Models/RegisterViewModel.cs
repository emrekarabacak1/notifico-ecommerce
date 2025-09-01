using System.ComponentModel.DataAnnotations;

namespace Notifico.Models
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Kullanıcı adı zorunludur!")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "E-posta zorunludur!")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta giriniz!")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Şifre zorunludur!")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required(ErrorMessage = "Ad zorunludur!")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Soyad zorunludur!")]
        public string LastName { get; set; }

        [Phone(ErrorMessage = "Geçerli bir telefon giriniz!")]
        public string PhoneNumber { get; set; }

        [DataType(DataType.Date)]
        public DateTime? BirthDate { get; set; }

        public string Address { get; set; }
    }
}
