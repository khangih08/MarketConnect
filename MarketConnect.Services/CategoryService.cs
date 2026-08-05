using MarketConnect.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarketConnect.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ApplicationDbContext _db;

        public CategoryService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IEnumerable<Category>> GetAllAsync()
        {
            return await _db.Categories.ToListAsync();
        }

        public async Task<Category?> GetByIdAsync(int id)
        {
            return await _db.Categories.FindAsync(id);
        }

        public async Task<Category> CreateAsync(Category category)
        {
            var exists = await _db.Categories.AnyAsync(c => c.Name.ToLower() == category.Name.ToLower());
            if (exists) throw new System.ArgumentException("Category with the same name already exists.");

            _db.Categories.Add(category);
            await _db.SaveChangesAsync();
            return category;
        }

        public async Task<Category?> UpdateAsync(int id, Category category)
        {
            var existing = await _db.Categories.FindAsync(id);
            if (existing == null) return null;

            var nameExists = await _db.Categories.AnyAsync(c => c.Id != id && c.Name.ToLower() == category.Name.ToLower());
            if (nameExists) throw new System.ArgumentException("Another category with the same name already exists.");

            existing.Name = category.Name;
            _db.Categories.Update(existing);
            await _db.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var category = await _db.Categories.FindAsync(id);
            if (category == null) return false;

            var hasProducts = await _db.Products.AnyAsync(p => p.CategoryId == id);
            if (hasProducts) throw new System.InvalidOperationException("Cannot delete this category because it contains active products. Please reassign or delete the products first.");

            _db.Categories.Remove(category);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
