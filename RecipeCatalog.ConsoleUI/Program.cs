using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RecipeCatalog.Data;
using RecipeCatalog.Services;
using RecipeCatalog.Services.Interfaces;

namespace RecipeCatalog.ConsoleUI
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var services = new ServiceCollection();

            services.AddDbContext<RecipeCatalogDbContext>(options =>
                options.UseSqlServer(
                    @"Server=(localdb)\MSSQLLocalDB;Database=RecipeCatalogDB;Trusted_Connection=True;"));

            services.AddScoped<IRecipeService, RecipeService>();
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<IReviewService, ReviewService>();

            var provider = services.BuildServiceProvider();

            using var scope = provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RecipeCatalogDbContext>();
            
            await db.Database.MigrateAsync();

            var recipeService = scope.ServiceProvider.GetRequiredService<IRecipeService>();
            var categoryService = scope.ServiceProvider.GetRequiredService<ICategoryService>();

            var menu = new ConsoleMenu
                (
                scope.ServiceProvider.GetRequiredService<IRecipeService>(),
                scope.ServiceProvider.GetRequiredService<ICategoryService>(), 
                scope.ServiceProvider.GetRequiredService<IReviewService>()
                );

            await menu.RunAsync();
        }
    }
}
