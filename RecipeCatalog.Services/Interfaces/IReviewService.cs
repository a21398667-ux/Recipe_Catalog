using RecipeCatalog.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecipeCatalog.Services.Interfaces
{
    public interface IReviewService
    {
        Task<IEnumerable<Review>> GetByRecipeAsync(int recipeId);
        Task<Review> CreateAsync(Review review);
        Task<bool> DeleteAsync(int id);

        /// Връща средната оценка за дадена рецепта
        Task<double> GetAverageRatingAsync(int recipeId);
    }
}
