using Microsoft.AspNetCore.Identity;

namespace Notifico.Models
{
    public class AppUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime? BirthDate { get; set; }
        public string Address { get; set; }
        public string? City { get; set; }
        public string? District { get; set; }
        public ICollection<FavoriteProduct> FavoriteProducts { get; set; } = new List<FavoriteProduct>();



    }
}
