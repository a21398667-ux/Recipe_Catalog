using Microsoft.EntityFrameworkCore;
using RecipeCatalog.Data;
using RecipeCatalog.Data.Models;
using RecipeCatalog.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecipeCatalog.Services
{
    public class ReviewService : IReviewService
    {
        private readonly RecipeCatalogDbContext _context;

        public ReviewService(RecipeCatalogDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Review>> GetByRecipeAsync(int recipeId)
        {
            return await _context.Reviews
                .Where(rv => rv.RecipeId == recipeId)
                .OrderByDescending(rv => rv.CreatedAt)
                .ToListAsync();
        }

        public async Task<Review> CreateAsync(Review review)
        {
            review.CreatedAt = DateTime.UtcNow;
            await _context.Reviews.AddAsync(review);
            await _context.SaveChangesAsync();
            return review;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null) return false;

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<double> GetAverageRatingAsync(int recipeId)
        {
            bool hasReviews = await _context.Reviews.AnyAsync(rv => rv.RecipeId == recipeId);
            if (!hasReviews) return 0;

            return await _context.Reviews
                .Where(rv => rv.RecipeId == recipeId)
                .AverageAsync(rv => rv.Rating);
        }
    }
}
