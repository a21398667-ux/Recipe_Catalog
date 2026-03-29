using Microsoft.EntityFrameworkCore;
using RecipeCatalog.Data;
using RecipeCatalog.Data.Models;
using RecipeCatalog.Services;
using Xunit;

namespace RecipeCatalog.Tests
{
    public class ReviewServiceTests : IDisposable
    {
        private readonly RecipeCatalogDbContext _context;
        private readonly ReviewService _service;

        public ReviewServiceTests()
        {
            var options = new DbContextOptionsBuilder<RecipeCatalogDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new RecipeCatalogDbContext(options);
            _service = new ReviewService(_context);

            _context.Categories.Add(new Category { Id = 1, Name = "Супи" });
            _context.Recipes.Add(new Recipe { Id = 1, Title = "Таратор", CategoryId = 1 });
            _context.Recipes.Add(new Recipe { Id = 2, Title = "Боб чорба", CategoryId = 1 });
            _context.SaveChanges();
        }

        [Fact]
        public async Task GetByRecipeAsync_ExistingRecipe_ReturnsReviews()
        {
            _context.Reviews.AddRange(
                new Review { RecipeId = 1, Rating = 5, Comment = "Страхотно!", ReviewerName = "Иван" },
                new Review { RecipeId = 1, Rating = 3, Comment = "Добро", ReviewerName = "Мария" }
            );
            await _context.SaveChangesAsync();

            var result = await _service.GetByRecipeAsync(1);

            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task GetByRecipeAsync_NoReviews_ReturnsEmptyList()
        {
            var result = await _service.GetByRecipeAsync(1);

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetByRecipeAsync_ReturnsOnlyReviewsForThatRecipe()
        {
            _context.Reviews.AddRange(
                new Review { RecipeId = 1, Rating = 5, ReviewerName = "Иван" },
                new Review { RecipeId = 2, Rating = 2, ReviewerName = "Петър" }
            );
            await _context.SaveChangesAsync();

            var result = await _service.GetByRecipeAsync(1);

            Assert.Single(result);
            Assert.All(result, r => Assert.Equal(1, r.RecipeId));
        }

        [Fact]
        public async Task GetByRecipeAsync_ReturnsReviewsOrderedByDateDescending()
        {
            var older = new Review { RecipeId = 1, Rating = 3, ReviewerName = "А", CreatedAt = DateTime.UtcNow.AddDays(-2) };
            var newer = new Review { RecipeId = 1, Rating = 5, ReviewerName = "Б", CreatedAt = DateTime.UtcNow };
            _context.Reviews.AddRange(older, newer);
            await _context.SaveChangesAsync();

            var result = (await _service.GetByRecipeAsync(1)).ToList();

            Assert.True(result[0].CreatedAt >= result[1].CreatedAt);
        }


        [Fact]
        public async Task CreateAsync_ValidReview_SavesAndReturnsReview()
        {
            var review = new Review
            {
                RecipeId = 1,
                Rating = 4,
                Comment = "Много добро!",
                ReviewerName = "Тест потребител"
            };

            var result = await _service.CreateAsync(review);

            Assert.True(result.Id > 0);
            Assert.Equal(4, result.Rating);
            Assert.Equal(1, await _context.Reviews.CountAsync());
        }

        [Fact]
        public async Task CreateAsync_SetsCreatedAtAutomatically()
        {
            var before = DateTime.UtcNow.AddSeconds(-1);
            var review = new Review { RecipeId = 1, Rating = 5, ReviewerName = "Тест" };

            var result = await _service.CreateAsync(review);

            Assert.True(result.CreatedAt >= before);
            Assert.True(result.CreatedAt <= DateTime.UtcNow.AddSeconds(1));
        }

        [Fact]
        public async Task CreateAsync_MultipleReviews_AllAreSaved()
        {
            await _service.CreateAsync(new Review { RecipeId = 1, Rating = 5, ReviewerName = "А" });
            await _service.CreateAsync(new Review { RecipeId = 1, Rating = 3, ReviewerName = "Б" });
            await _service.CreateAsync(new Review { RecipeId = 1, Rating = 4, ReviewerName = "В" });

            Assert.Equal(3, await _context.Reviews.CountAsync());
        }

        [Fact]
        public async Task DeleteAsync_ExistingId_DeletesSuccessfully()
        {
            var review = new Review { RecipeId = 1, Rating = 4, ReviewerName = "За изтриване" };
            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            var success = await _service.DeleteAsync(review.Id);

            Assert.True(success);
            Assert.Equal(0, await _context.Reviews.CountAsync());
        }

        [Fact]
        public async Task DeleteAsync_NonExistingId_ReturnsFalse()
        {
            var success = await _service.DeleteAsync(999);

            Assert.False(success);
        }

        [Fact]
        public async Task DeleteAsync_DeletesOnlyTargetReview()
        {
            var review1 = new Review { RecipeId = 1, Rating = 5, ReviewerName = "Иван" };
            var review2 = new Review { RecipeId = 1, Rating = 3, ReviewerName = "Мария" };
            _context.Reviews.AddRange(review1, review2);
            await _context.SaveChangesAsync();

            var success = await _service.DeleteAsync(review1.Id);

            Assert.True(success);
            Assert.Equal(1, await _context.Reviews.CountAsync());
            var remaining = await _context.Reviews.FirstAsync();
            Assert.Equal(review2.Id, remaining.Id);
        }


        [Fact]
        public async Task GetAverageRatingAsync_NoReviews_ReturnsZero()
        {
            var avg = await _service.GetAverageRatingAsync(1);

            Assert.Equal(0, avg);
        }

        [Fact]
        public async Task GetAverageRatingAsync_SingleReview_ReturnsThatRating()
        {
            _context.Reviews.Add(new Review { RecipeId = 1, Rating = 4, ReviewerName = "Иван" });
            await _context.SaveChangesAsync();

            var avg = await _service.GetAverageRatingAsync(1);

            Assert.Equal(4.0, avg);
        }

        [Fact]
        public async Task GetAverageRatingAsync_MultipleReviews_ReturnsCorrectAverage()
        {
            _context.Reviews.AddRange(
                new Review { RecipeId = 1, Rating = 5, ReviewerName = "А" },
                new Review { RecipeId = 1, Rating = 3, ReviewerName = "Б" },
                new Review { RecipeId = 1, Rating = 4, ReviewerName = "В" }
            );
            await _context.SaveChangesAsync();

            var avg = await _service.GetAverageRatingAsync(1);

            Assert.Equal(4.0, avg, precision: 5);
        }

        [Fact]
        public async Task GetAverageRatingAsync_OnlyCountsReviewsForThatRecipe()
        {
            _context.Reviews.AddRange(
                new Review { RecipeId = 1, Rating = 5, ReviewerName = "А" },
                new Review { RecipeId = 2, Rating = 1, ReviewerName = "Б" } // друга рецепта
            );
            await _context.SaveChangesAsync();

            var avg = await _service.GetAverageRatingAsync(1);

            Assert.Equal(5.0, avg);
        }

        public void Dispose() => _context.Dispose();
    }
}
