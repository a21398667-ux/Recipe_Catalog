using Microsoft.EntityFrameworkCore;
using RecipeCatalog.Data;
using RecipeCatalog.Data.Models;
using RecipeCatalog.Services;
using Xunit;

namespace RecipeCatalog.Tests
{
    public class CategoryServiceTests : IDisposable
    {
        private readonly RecipeCatalogDbContext _context;
        private readonly CategoryService _service;

        public CategoryServiceTests()
        {
            var options = new DbContextOptionsBuilder<RecipeCatalogDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new RecipeCatalogDbContext(options);
            _service = new CategoryService(_context);
        }


        [Fact]
        public async Task GetAllAsync_ReturnsAllCategories()
        {
            _context.Categories.AddRange(
                new Category { Name = "Супи" },
                new Category { Name = "Десерти" }
            );
            await _context.SaveChangesAsync();

            var result = await _service.GetAllAsync();
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task GetAllAsync_EmptyDatabase_ReturnsEmptyList()
        {
            var result = await _service.GetAllAsync();
            Assert.Empty(result);
        }


        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsCategory()
        {
            var category = new Category { Name = "Салати", Description = "Свежи салати" };
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            var result = await _service.GetByIdAsync(category.Id);

            Assert.NotNull(result);
            Assert.Equal("Салати", result.Name);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingId_ReturnsNull()
        {
            var result = await _service.GetByIdAsync(999);
            Assert.Null(result);
        }


        [Fact]
        public async Task CreateAsync_ValidCategory_SavesSuccessfully()
        {
            var category = new Category { Name = "Салати", Description = "Свежи салати" };
            var result = await _service.CreateAsync(category);

            Assert.True(result.Id > 0);
            Assert.Equal("Салати", result.Name);
            Assert.Equal(1, await _context.Categories.CountAsync());
        }

        [Fact]
        public async Task CreateAsync_SetsCorrectDescription()
        {
            var category = new Category { Name = "Основни", Description = "Основни ястия" };
            var result = await _service.CreateAsync(category);

            Assert.Equal("Основни ястия", result.Description);
        }


        [Fact]
        public async Task UpdateAsync_ExistingCategory_UpdatesName()
        {
            var category = new Category { Name = "Старо" };
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            category.Name = "Ново";
            var success = await _service.UpdateAsync(category);

            Assert.True(success);
            var updated = await _context.Categories.FindAsync(category.Id);
            Assert.Equal("Ново", updated!.Name);
        }

        [Fact]
        public async Task UpdateAsync_ExistingCategory_UpdatesDescription()
        {
            var category = new Category { Name = "Тест", Description = "Стара" };
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            category.Description = "Нова описание";
            var success = await _service.UpdateAsync(category);

            Assert.True(success);
            var updated = await _context.Categories.FindAsync(category.Id);
            Assert.Equal("Нова описание", updated!.Description);
        }

        [Fact]
        public async Task UpdateAsync_NonExistingId_ReturnsFalse()
        {
            var category = new Category { Id = 999, Name = "Несъществуваща" };
            var success = await _service.UpdateAsync(category);
            Assert.False(success);
        }


        [Fact]
        public async Task DeleteAsync_ExistingId_DeletesSuccessfully()
        {
            var category = new Category { Name = "За изтриване" };
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            var success = await _service.DeleteAsync(category.Id);

            Assert.True(success);
            Assert.Equal(0, await _context.Categories.CountAsync());
        }

        [Fact]
        public async Task DeleteAsync_NonExistingId_ReturnsFalse()
        {
            var success = await _service.DeleteAsync(999);
            Assert.False(success);
        }

        [Fact]
        public async Task DeleteAsync_DeletesOnlyTargetCategory()
        {
            var cat1 = new Category { Name = "Супи" };
            var cat2 = new Category { Name = "Десерти" };
            _context.Categories.AddRange(cat1, cat2);
            await _context.SaveChangesAsync();

            await _service.DeleteAsync(cat1.Id);

            Assert.Equal(1, await _context.Categories.CountAsync());
            var remaining = await _context.Categories.FirstAsync();
            Assert.Equal("Десерти", remaining.Name);
        }

        public void Dispose() => _context.Dispose();
    }
}
