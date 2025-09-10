using Notifico.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Notifico.Services
{
    public interface IProductService
    {
        Task<List<Product>> GetAllProductsAsync(string search, string category);
        Task<List<Product>> GetAllProductsAsync();                
        Task<Product> GetProductByIdAsync(int id);
        Task<List<string>> GetAllCategoriesAsync();
        Task<List<int>> GetFavoriteProductIdsAsync(string userId);
        Task<bool> IsProductFavoriteAsync(string userId, int productId);
        Task ToggleFavoriteAsync(string userId, int productId);
        Task<List<FavoriteProduct>> GetUserFavoritesAsync(string userId);
        Task AddProductAsync(Product product);
        Task UpdateProductAsync(Product product);
        Task DeleteProductAsync(Product product);
        Task<bool> ExistsByNameAsync(string name, int? exceptId = null);
    }
}
