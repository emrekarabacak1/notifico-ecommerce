using System.ComponentModel.DataAnnotations;

namespace Notifico.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Kullanıcı adı zorunludur!")]
        [StringLength(30, ErrorMessage = "Kullanıcı adı en fazla 30 karakter olabilir!")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Email zorunludur!")]
        [EmailAddress(ErrorMessage = "Geçerli bir email giriniz!")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Şifre zorunludur!")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Şifre en az 6 karakter olmalı!")]
        public string PasswordHash { get; set; }
        public string Role { get; set; }
        public string Salt { get; set; }
        public DateTime DateCreated { get; set; }
    }
}
