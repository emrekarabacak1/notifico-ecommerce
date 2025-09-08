using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Notifico.Models
{
    public class ProductFormViewModel
    {
        public int Id { get; set; }

        [Required, StringLength(120)]
        public string Name { get; set; }

        [StringLength(300)]
        public string Description { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        [Range(0, int.MaxValue)]
        public int Stock { get; set; }

        [StringLength(70)]
        public string Category { get; set; }

        [Display(Name = "Görsel URL")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Görsel Dosyası")]
        public IFormFile? ImageFile { get; set; }
    }
}
