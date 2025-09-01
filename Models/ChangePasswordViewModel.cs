using System.ComponentModel.DataAnnotations;

namespace Notifico.Models
{
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Mevcut şifre zorunludur!")]
        [DataType(DataType.Password)]
        public string OldPassword { get; set; }

        [Required(ErrorMessage = "Yeni şifre zorunludur!")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Yeni şifre en az 6 karakter olmalı!")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Şifre tekrarı zorunludur!")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Şifreler uyuşmuyor!")]
        public string ConfirmPassword { get; set; }
    }
}
