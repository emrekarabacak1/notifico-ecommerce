using Notifico.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Notifico.Repositories
{
    public interface IProductRepository
    {
        Task<List<Product>> GetAllAsync(string search, string category);
        Task<Product> GetByIdAsync(int id);
        Task<List<string>> GetAllCategoriesAsync();
        Task<List<int>> GetFavoriteProductIdsAsync(string userId);
        Task<bool> IsProductFavoriteAsync(string userId, int productId);
        Task AddFavoriteAsync(string userId, int productId);
        Task RemoveFavoriteAsync(string userId, int productId);
        Task<List<FavoriteProduct>> GetUserFavoritesAsync(string userId);
        Task<List<Product>> GetAllAsync(); 
        Task AddAsync(Product product);    
        Task UpdateAsync(Product product); 
        Task DeleteAsync(Product product); 
        Task<bool> ExistsByNameAsync(string name, int? exceptId = null); 
    }
}
