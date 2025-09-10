using Notifico.Models;
using Notifico.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Notifico.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;

        public ProductService(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<List<Product>> GetAllProductsAsync(string search, string category)
        {
            return await _productRepository.GetAllAsync(search, category);
        }
        public async Task<List<Product>> GetAllProductsAsync()
        {
            return await _productRepository.GetAllAsync();
        }

        public async Task<Product> GetProductByIdAsync(int id)
        {
            return await _productRepository.GetByIdAsync(id);
        }

        public async Task<List<string>> GetAllCategoriesAsync()
        {
            return await _productRepository.GetAllCategoriesAsync();
        }

        public async Task<List<int>> GetFavoriteProductIdsAsync(string userId)
        {
            return await _productRepository.GetFavoriteProductIdsAsync(userId);
        }

        public async Task<bool> IsProductFavoriteAsync(string userId, int productId)
        {
            return await _productRepository.IsProductFavoriteAsync(userId, productId);
        }

        public async Task ToggleFavoriteAsync(string userId, int productId)
        {
            var isFavorite = await _productRepository.IsProductFavoriteAsync(userId, productId);
            if (isFavorite)
                await _productRepository.RemoveFavoriteAsync(userId, productId);
            else
                await _productRepository.AddFavoriteAsync(userId, productId);
        }

        public async Task<List<FavoriteProduct>> GetUserFavoritesAsync(string userId)
        {
            return await _productRepository.GetUserFavoritesAsync(userId);
        }
        public async Task AddProductAsync(Product product)
        {
            await _productRepository.AddAsync(product);
        }
        public async Task UpdateProductAsync(Product product)
        {
            await _productRepository.UpdateAsync(product);
        }
        public async Task DeleteProductAsync(Product product)
        {
            await _productRepository.DeleteAsync(product);
        }
        public async Task<bool> ExistsByNameAsync(string name, int? exceptId = null)
        {
            return await _productRepository.ExistsByNameAsync(name, exceptId);
        }
    }
}
