using Notifico.Models;

namespace Notifico.Models
{
    public class Address
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public AppUser User { get; set; }
        public string Title { get; set; }
        public string FullAddress { get; set; }
        public string City { get; set; }
        public string District { get; set; }
        public string ZipCode { get; set; }
        public bool IsDefault { get; set; }
    }
}
