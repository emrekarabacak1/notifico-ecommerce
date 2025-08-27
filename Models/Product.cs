using System.ComponentModel.DataAnnotations;

namespace Notifico.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Ürün adı zorunludur!")]
        [StringLength(50, ErrorMessage = "Ürün adı en fazla 50 karakter olabilir!")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Açıklama zorunludur!")]
        [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir!")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Fiyat zorunludur!")]
        [Range(0.01, 999999, ErrorMessage = "Fiyat pozitif olmalıdır!")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Stok zorunludur!")]
        [Range(0, 99999, ErrorMessage = "Stok sıfır veya daha büyük olmalıdır!")]
        public int Stock { get; set; }

        [Url(ErrorMessage = "Geçerli bir resim adresi giriniz!")]
        public string ImageUrl { get; set; }

        [Required(ErrorMessage = "Kategori zorunludur!")]
        [StringLength(30, ErrorMessage = "Kategori en fazla 30 karakter olabilir!")]
        public string Category { get; set; }
        public DateTime DateAdded { get; set; }
    }
}
