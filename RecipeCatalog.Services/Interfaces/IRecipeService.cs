using RecipeCatalog.Data.Models;

namespace RecipeCatalog.Services.Interfaces
{
    public interface IRecipeService
    {
        Task<IEnumerable<Recipe>> GetAllAsync();
        Task<Recipe?> GetByIdAsync(int id);
        Task<IEnumerable<Recipe>> GetByCategoryAsync(int categoryId);
        Task<IEnumerable<Recipe>> SearchAsync(string keyword);
        Task<Recipe> CreateAsync(Recipe recipe);
        Task<bool> UpdateAsync(Recipe recipe);
        Task<bool> DeleteAsync(int id);
    }
}
