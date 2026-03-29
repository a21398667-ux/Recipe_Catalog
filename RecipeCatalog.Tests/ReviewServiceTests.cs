using Microsoft.EntityFrameworkCore;
using RecipeCatalog.Data;
using RecipeCatalog.Data.Models;
using RecipeCatalog.Services;
using Xunit;

namespace RecipeCatalog.Tests
{
    /// <summary>
    /// Компонентни тестове за ReviewService.
    /// </summary>
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

            // Seed данни
            _context.Categories.Add(new Category { Id = 1, Name = "Супи" });
            _context.Recipes.Add(new Recipe { Id = 1, Title = "Таратор", CategoryId = 1 });
            _context.Recipes.Add(new Recipe { Id = 2, Title = "Боб чорба", CategoryId = 1 });
            _context.SaveChanges();
        }

        // ── GetByRecipeAsync ────────────────────────────────────────────────

        [Fact]
        public async Task GetByRecipeAsync_ExistingRecipe_ReturnsReviews()
        {
            // Arrange
            _context.Reviews.AddRange(
                new Review { RecipeId = 1, Rating = 5, Comment = "Страхотно!", ReviewerName = "Иван" },
                new Review { RecipeId = 1, Rating = 3, Comment = "Добро", ReviewerName = "Мария" }
            );
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetByRecipeAsync(1);

            // Assert
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task GetByRecipeAsync_NoReviews_ReturnsEmptyList()
        {
            // Act
            var result = await _service.GetByRecipeAsync(1);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetByRecipeAsync_ReturnsOnlyReviewsForThatRecipe()
        {
            // Arrange
            _context.Reviews.AddRange(
                new Review { RecipeId = 1, Rating = 5, ReviewerName = "Иван" },
                new Review { RecipeId = 2, Rating = 2, ReviewerName = "Петър" }
            );
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetByRecipeAsync(1);

            // Assert
            Assert.Single(result);
            Assert.All(result, r => Assert.Equal(1, r.RecipeId));
        }

        [Fact]
        public async Task GetByRecipeAsync_ReturnsReviewsOrderedByDateDescending()
        {
            // Arrange
            var older = new Review { RecipeId = 1, Rating = 3, ReviewerName = "А", CreatedAt = DateTime.UtcNow.AddDays(-2) };
            var newer = new Review { RecipeId = 1, Rating = 5, ReviewerName = "Б", CreatedAt = DateTime.UtcNow };
            _context.Reviews.AddRange(older, newer);
            await _context.SaveChangesAsync();

            // Act
            var result = (await _service.GetByRecipeAsync(1)).ToList();

            // Assert
            Assert.True(result[0].CreatedAt >= result[1].CreatedAt);
        }

        // ── CreateAsync ─────────────────────────────────────────────────────

        [Fact]
        public async Task CreateAsync_ValidReview_SavesAndReturnsReview()
        {
            // Arrange
            var review = new Review
            {
                RecipeId = 1,
                Rating = 4,
                Comment = "Много добро!",
                ReviewerName = "Тест потребител"
            };

            // Act
            var result = await _service.CreateAsync(review);

            // Assert
            Assert.True(result.Id > 0);
            Assert.Equal(4, result.Rating);
            Assert.Equal(1, await _context.Reviews.CountAsync());
        }

        [Fact]
        public async Task CreateAsync_SetsCreatedAtAutomatically()
        {
            // Arrange
            var before = DateTime.UtcNow.AddSeconds(-1);
            var review = new Review { RecipeId = 1, Rating = 5, ReviewerName = "Тест" };

            // Act
            var result = await _service.CreateAsync(review);

            // Assert
            Assert.True(result.CreatedAt >= before);
            Assert.True(result.CreatedAt <= DateTime.UtcNow.AddSeconds(1));
        }

        [Fact]
        public async Task CreateAsync_MultipleReviews_AllAreSaved()
        {
            // Arrange & Act
            await _service.CreateAsync(new Review { RecipeId = 1, Rating = 5, ReviewerName = "А" });
            await _service.CreateAsync(new Review { RecipeId = 1, Rating = 3, ReviewerName = "Б" });
            await _service.CreateAsync(new Review { RecipeId = 1, Rating = 4, ReviewerName = "В" });

            // Assert
            Assert.Equal(3, await _context.Reviews.CountAsync());
        }

        // ── DeleteAsync ─────────────────────────────────────────────────────

        [Fact]
        public async Task DeleteAsync_ExistingId_DeletesSuccessfully()
        {
            // Arrange
            var review = new Review { RecipeId = 1, Rating = 4, ReviewerName = "За изтриване" };
            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            // Act
            var success = await _service.DeleteAsync(review.Id);

            // Assert
            Assert.True(success);
            Assert.Equal(0, await _context.Reviews.CountAsync());
        }

        [Fact]
        public async Task DeleteAsync_NonExistingId_ReturnsFalse()
        {
            // Act
            var success = await _service.DeleteAsync(999);

            // Assert
            Assert.False(success);
        }

        [Fact]
        public async Task DeleteAsync_DeletesOnlyTargetReview()
        {
            // Arrange
            var review1 = new Review { RecipeId = 1, Rating = 5, ReviewerName = "Иван" };
            var review2 = new Review { RecipeId = 1, Rating = 3, ReviewerName = "Мария" };
            _context.Reviews.AddRange(review1, review2);
            await _context.SaveChangesAsync();

            // Act
            var success = await _service.DeleteAsync(review1.Id);

            // Assert
            Assert.True(success);
            Assert.Equal(1, await _context.Reviews.CountAsync());
            var remaining = await _context.Reviews.FirstAsync();
            Assert.Equal(review2.Id, remaining.Id);
        }

        // ── GetAverageRatingAsync ────────────────────────────────────────────

        [Fact]
        public async Task GetAverageRatingAsync_NoReviews_ReturnsZero()
        {
            // Act
            var avg = await _service.GetAverageRatingAsync(1);

            // Assert
            Assert.Equal(0, avg);
        }

        [Fact]
        public async Task GetAverageRatingAsync_SingleReview_ReturnsThatRating()
        {
            // Arrange
            _context.Reviews.Add(new Review { RecipeId = 1, Rating = 4, ReviewerName = "Иван" });
            await _context.SaveChangesAsync();

            // Act
            var avg = await _service.GetAverageRatingAsync(1);

            // Assert
            Assert.Equal(4.0, avg);
        }

        [Fact]
        public async Task GetAverageRatingAsync_MultipleReviews_ReturnsCorrectAverage()
        {
            // Arrange
            _context.Reviews.AddRange(
                new Review { RecipeId = 1, Rating = 5, ReviewerName = "А" },
                new Review { RecipeId = 1, Rating = 3, ReviewerName = "Б" },
                new Review { RecipeId = 1, Rating = 4, ReviewerName = "В" }
            );
            await _context.SaveChangesAsync();

            // Act
            var avg = await _service.GetAverageRatingAsync(1);

            // Assert
            Assert.Equal(4.0, avg, precision: 5);
        }

        [Fact]
        public async Task GetAverageRatingAsync_OnlyCountsReviewsForThatRecipe()
        {
            // Arrange
            _context.Reviews.AddRange(
                new Review { RecipeId = 1, Rating = 5, ReviewerName = "А" },
                new Review { RecipeId = 2, Rating = 1, ReviewerName = "Б" } // друга рецепта
            );
            await _context.SaveChangesAsync();

            // Act
            var avg = await _service.GetAverageRatingAsync(1);

            // Assert
            Assert.Equal(5.0, avg);
        }

        public void Dispose() => _context.Dispose();
    }
}
