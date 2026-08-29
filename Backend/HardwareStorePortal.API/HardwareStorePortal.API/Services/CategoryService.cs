using Microsoft.EntityFrameworkCore;
using HardwareStorePortal.API.Data;
using HardwareStorePortal.API.DTOs;
using HardwareStorePortal.API.Models;

namespace HardwareStorePortal.API.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly AppDbContext _context;

        public CategoryService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<CategoryDTO>> GetAllAsync()
        {
            return await _context.Categories
                .OrderBy(c => c.Name)
                .Select(c => new CategoryDTO { Id = c.Id, Name = c.Name })
                .ToListAsync();
        }

        public async Task<CategoryDTO> CreateAsync(CreateCategoryDTO dto)
        {
            var trimmedName = dto.Name.Trim();

            var existing = await _context.Categories
                .FirstOrDefaultAsync(c => c.Name.ToLower() == trimmedName.ToLower());

            if (existing != null)
                throw new InvalidOperationException("A category with this name already exists.");

            var category = new Category { Name = trimmedName };
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return new CategoryDTO { Id = category.Id, Name = category.Name };
        }

        public async Task DeleteAsync(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                throw new InvalidOperationException("Category not found.");

            var isInUse = await _context.Products.AnyAsync(p => p.CategoryId == id);
            if (isInUse)
                throw new InvalidOperationException("Cannot delete this category because it's assigned to one or more products.");

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
        }
    }
}