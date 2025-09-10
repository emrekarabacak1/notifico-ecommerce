using Notifico.Data;
using Notifico.Models;
using Microsoft.EntityFrameworkCore;

namespace Notifico.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly ApplicationDbContext _context;

        public ProductRepository(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<List<Product>> GetAllAsync(string search, string category)
        {
            var query = _context.Products.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p => p.Name.ToLower().Contains(search.ToLower()));

            if (!string.IsNullOrWhiteSpace(category))
                query = query.Where(p => p.Category.ToLower() == category.ToLower());

            return await query.ToListAsync();
        }
        public async Task<List<Product>> GetAllAsync()
        {
            return await _context.Products.ToListAsync();
        }

        public async Task<Product> GetByIdAsync(int id)
        {
            return await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<List<string>> GetAllCategoriesAsync()
        {
            return await _context.Products
                .Select(p => p.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
        }

        public async Task<List<int>> GetFavoriteProductIdsAsync(string userId)
        {
            return await _context.FavoriteProducts
                .Where(f => f.UserId == userId)
                .Select(f => f.ProductId)
                .ToListAsync();
        }

        public async Task<bool> IsProductFavoriteAsync(string userId, int productId)
        {
            return await _context.FavoriteProducts
                .AnyAsync(f => f.UserId == userId && f.ProductId == productId);
        }

        public async Task AddFavoriteAsync(string userId, int productId)
        {
            _context.FavoriteProducts.Add(new FavoriteProduct
            {
                UserId = userId,
                ProductId = productId,
                DateAdded = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }

        public async Task RemoveFavoriteAsync(string userId, int productId)
        {
            var favorite = await _context.FavoriteProducts
                .FirstOrDefaultAsync(f => f.UserId == userId && f.ProductId == productId);
            if (favorite != null)
            {
                _context.FavoriteProducts.Remove(favorite);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<FavoriteProduct>> GetUserFavoritesAsync(string userId)
        {
            return await _context.FavoriteProducts
                .Where(f => f.UserId == userId)
                .Include(f => f.Product)
                .OrderByDescending(f => f.DateAdded)
                .ToListAsync();
        }
        public async Task AddAsync(Product product)
        {
            await _context.Products.AddAsync(product);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Product product)
        {
            _context.Products.Update(product);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Product product)
        {
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ExistsByNameAsync(string name, int? exceptId = null)
        {
            return await _context.Products.AnyAsync(p =>
                p.Name.ToLower() == name.ToLower() &&
                (!exceptId.HasValue || p.Id != exceptId.Value)
            );
        }
    }
}
