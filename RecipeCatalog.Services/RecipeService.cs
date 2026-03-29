using Microsoft.EntityFrameworkCore;
using RecipeCatalog.Data;
using RecipeCatalog.Data.Models;
using RecipeCatalog.Services.Interfaces;

namespace RecipeCatalog.Services
{
    public class RecipeService : IRecipeService
    {
        private readonly RecipeCatalogDbContext _context;

        public RecipeService(RecipeCatalogDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Recipe>> GetAllAsync()
        {
            return await _context.Recipes
                .Include(r => r.Category)
                .Include(r => r.Ingredients)
                .ToListAsync();
        }

        public async Task<Recipe?> GetByIdAsync(int id)
        {
            return await _context.Recipes
                .Include(r => r.Category)
                .Include(r => r.Ingredients)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<IEnumerable<Recipe>> GetByCategoryAsync(int categoryId)
        {
            return await _context.Recipes
                .Include(r => r.Category)
                .Include(r => r.Ingredients)
                .Where(r => r.CategoryId == categoryId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Recipe>> SearchAsync(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return await GetAllAsync();
            }

            var lower = keyword.ToLower();
            return await _context.Recipes
                .Include(r => r.Category)
                .Include(r => r.Ingredients)
                .Where(r => r.Title.ToLower().Contains(lower) ||
                            r.Description.ToLower().Contains(lower))
                .ToListAsync();
        }

        public async Task<Recipe> CreateAsync(Recipe recipe)
        {
            _context.Recipes.Add(recipe);
            await _context.SaveChangesAsync();
            return recipe;
        }

        public async Task<bool> UpdateAsync(Recipe recipe)
        {
            var existing = await _context.Recipes
                .Include(r => r.Ingredients)
                .FirstOrDefaultAsync(r => r.Id == recipe.Id);

            if (existing == null)
            {
                return false;
            }

            existing.Title = recipe.Title;
            existing.Description = recipe.Description;
            existing.Instructions = recipe.Instructions;
            existing.PreparationTimeMinutes = recipe.PreparationTimeMinutes;
            existing.CookingTimeMinutes = recipe.CookingTimeMinutes;
            existing.Servings = recipe.Servings;
            existing.CategoryId = recipe.CategoryId;

            _context.Ingredients.RemoveRange(existing.Ingredients);
            existing.Ingredients = recipe.Ingredients;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var recipe = await _context.Recipes.FindAsync(id);

            if (recipe == null)
            {
                return false;
            }

            _context.Recipes.Remove(recipe);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
