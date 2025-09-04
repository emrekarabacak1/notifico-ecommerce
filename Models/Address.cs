using System.ComponentModel.DataAnnotations;

namespace Notifico.Models
{
    public class Address
    {
        public int Id { get; set; }

        public string UserId { get; set; }

        public AppUser User { get; set; }

        [Required(ErrorMessage = "Başlık zorunludur.")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Adres detayı zorunludur.")]
        public string FullAddress { get; set; }

        [Required(ErrorMessage = "Şehir seçimi zorunludur.")]
        public string City { get; set; }

        [Required(ErrorMessage = "İlçe seçimi zorunludur.")]
        public string District { get; set; }

        [Required(ErrorMessage = "Posta kodu zorunludur.")]
        public string ZipCode { get; set; }

        public bool IsDefault { get; set; }
    }
}
