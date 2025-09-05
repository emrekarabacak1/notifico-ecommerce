using System.ComponentModel.DataAnnotations;

namespace Notifico.Models
{
    public class ProfileViewModel
    {
        [Required(ErrorMessage = "Kullanıcı adı zorunludur!")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "E-posta zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz!")]
        public string Email { get; set; }

        [Display(Name = "Ad")]
        [Required(ErrorMessage = "Ad zorunludur.")]
        public string FirstName { get; set; }

        [Display(Name = "Soyad")]
        [Required(ErrorMessage = "Soyad zorunludur.")]
        public string LastName { get; set; }

        [Display(Name = "Telefon")]
        [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz!")]
        public string PhoneNumber { get; set; }

        [Display(Name = "Doğum Tarihi")]
        [DataType(DataType.Date)]
        public DateTime? BirthDate { get; set; }

        [Display(Name = "Adres")]
        public string Address { get; set; }

        [Display(Name = "Şehir")]
        public string City { get; set; }

        [Display(Name = "İlçe")]
        public string District { get; set; }
    }
}
