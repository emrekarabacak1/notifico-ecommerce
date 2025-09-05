using System.ComponentModel.DataAnnotations;

namespace Notifico.Models
{
    public class ProfileEditViewModel
    {
        [Required(ErrorMessage = "Ad zorunludur.")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Soyad zorunludur.")]
        public string LastName { get; set; }

        [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz!")]
        public string PhoneNumber { get; set; }

        [DataType(DataType.Date)]
        public DateTime? BirthDate { get; set; }

        [Display(Name = "Adres Detayı")]
        public string Address { get; set; }

        [Display(Name = "Şehir")]
        public string City { get; set; }

        [Display(Name = "İlçe")]
        public string District { get; set; }
    }
}
