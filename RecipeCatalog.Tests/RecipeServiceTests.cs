using Microsoft.EntityFrameworkCore;
using RecipeCatalog.Data;
using RecipeCatalog.Data.Models;
using RecipeCatalog.Services;
using Xunit;

namespace RecipeCatalog.Tests
{
    /// <summary>
    /// Компонентни тестове за RecipeService.
    /// </summary>
    public class RecipeServiceTests : IDisposable
    {
        private readonly RecipeCatalogDbContext _context;
        private readonly RecipeService _service;

        public RecipeServiceTests()
        {
            // Use in-memory database for tests
            var options = new DbContextOptionsBuilder<RecipeCatalogDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new RecipeCatalogDbContext(options);
            _service = new RecipeService(_context);

            // Seed test data
            _context.Categories.Add(new Category { Id = 1, Name = "Супи", Description = "Топли супи" });
            _context.Categories.Add(new Category { Id = 2, Name = "Десерти", Description = "Сладкиши" });
            _context.SaveChanges();
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAllRecipes()
        {
            // Arrange
            _context.Recipes.AddRange(
                new Recipe { Title = "Таратор", CategoryId = 1 },
                new Recipe { Title = "Баница", CategoryId = 2 }
            );
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetAllAsync();

            // Assert
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsRecipe()
        {
            // Arrange
            var recipe = new Recipe { Title = "Боб чорба", CategoryId = 1, Servings = 4 };
            _context.Recipes.Add(recipe);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetByIdAsync(recipe.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Боб чорба", result.Title);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingId_ReturnsNull()
        {
            // Act
            var result = await _service.GetByIdAsync(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task CreateAsync_ValidRecipe_SavesAndReturnsRecipe()
        {
            // Arrange
            var recipe = new Recipe
            {
                Title = "Шкембе чорба",
                Description = "Традиционна супа",
                Instructions = "Свари шкембето...",
                PreparationTimeMinutes = 30,
                CookingTimeMinutes = 120,
                Servings = 6,
                CategoryId = 1
            };

            // Act
            var result = await _service.CreateAsync(recipe);

            // Assert
            Assert.True(result.Id > 0);
            Assert.Equal("Шкембе чорба", result.Title);
            Assert.Equal(1, await _context.Recipes.CountAsync());
        }

        [Fact]
        public async Task UpdateAsync_ExistingRecipe_UpdatesSuccessfully()
        {
            // Arrange
            var recipe = new Recipe { Title = "Стара рецепта", CategoryId = 1 };
            _context.Recipes.Add(recipe);
            await _context.SaveChangesAsync();

            recipe.Title = "Нова рецепта";

            // Act
            var success = await _service.UpdateAsync(recipe);

            // Assert
            Assert.True(success);
            var updated = await _context.Recipes.FindAsync(recipe.Id);
            Assert.Equal("Нова рецепта", updated!.Title);
        }

        [Fact]
        public async Task UpdateAsync_NonExistingRecipe_ReturnsFalse()
        {
            // Arrange
            var recipe = new Recipe { Id = 999, Title = "Несъществуваща", CategoryId = 1 };

            // Act
            var success = await _service.UpdateAsync(recipe);

            // Assert
            Assert.False(success);
        }

        [Fact]
        public async Task DeleteAsync_ExistingId_DeletesSuccessfully()
        {
            // Arrange
            var recipe = new Recipe { Title = "За изтриване", CategoryId = 1 };
            _context.Recipes.Add(recipe);
            await _context.SaveChangesAsync();

            // Act
            var success = await _service.DeleteAsync(recipe.Id);

            // Assert
            Assert.True(success);
            Assert.Equal(0, await _context.Recipes.CountAsync());
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
        public async Task SearchAsync_MatchingKeyword_ReturnsFilteredRecipes()
        {
            // Arrange
            _context.Recipes.AddRange(
                new Recipe { Title = "Таратор", Description = "Студена супа", CategoryId = 1 },
                new Recipe { Title = "Боб чорба", Description = "Топла супа", CategoryId = 1 },
                new Recipe { Title = "Шоколадова торта", Description = "Десерт", CategoryId = 2 }
            );
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.SearchAsync("супа");

            // Assert
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task SearchAsync_EmptyKeyword_ReturnsAllRecipes()
        {
            // Arrange
            _context.Recipes.AddRange(
                new Recipe { Title = "Рецепта 1", CategoryId = 1 },
                new Recipe { Title = "Рецепта 2", CategoryId = 2 }
            );
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.SearchAsync("");

            // Assert
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task GetByCategoryAsync_ReturnsOnlyMatchingCategory()
        {
            // Arrange
            _context.Recipes.AddRange(
                new Recipe { Title = "Таратор", CategoryId = 1 },
                new Recipe { Title = "Боб чорба", CategoryId = 1 },
                new Recipe { Title = "Торта", CategoryId = 2 }
            );
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetByCategoryAsync(1);

            // Assert
            Assert.Equal(2, result.Count());
            Assert.All(result, r => Assert.Equal(1, r.CategoryId));
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
