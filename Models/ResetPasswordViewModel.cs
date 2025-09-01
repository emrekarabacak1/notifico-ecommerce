using System.ComponentModel.DataAnnotations;

namespace Notifico.Models
{
    public class ResetPasswordViewModel
    {
        [Required(ErrorMessage = "E-posta zorunludur!")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta giriniz!")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Yeni şifre zorunludur!")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Şifre en az 6 karakter olmalı!")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required(ErrorMessage = "Şifre tekrar zorunludur!")]
        [Compare("Password", ErrorMessage = "Şifreler eşleşmiyor!")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }

        public string Token { get; set; }
    }
}
