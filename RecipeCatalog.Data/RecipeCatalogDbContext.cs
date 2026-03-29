using Microsoft.EntityFrameworkCore;
using RecipeCatalog.Data.Models;

namespace RecipeCatalog.Data
{
    public class RecipeCatalogDbContext : DbContext
    {
        public RecipeCatalogDbContext()
        {
        }

        public RecipeCatalogDbContext(DbContextOptions<RecipeCatalogDbContext> options)
            : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(
                    @"Server=(localdb)\MSSQLLocalDB;Database=RecipeCatalogDB;Trusted_Connection=True;");
            }
        }

        public DbSet<Recipe> Recipes => Set<Recipe>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Ingredient> Ingredients => Set<Ingredient>();
        public DbSet<Review> Reviews => Set<Review>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Recipe>()
                .HasOne(r => r.Category)
                .WithMany(c => c.Recipes)
                .HasForeignKey(r => r.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Ingredient>()
                .HasOne(i => i.Recipe)
                .WithMany(r => r.Ingredients)
                .HasForeignKey(i => i.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Review>()
                .HasOne(rv => rv.Recipe)
                .WithMany(r => r.Reviews)
                .HasForeignKey(rv => rv.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Супи", Description = "Топли и студени супи" },
                new Category { Id = 2, Name = "Основни ястия", Description = "Основни ястия за обяд и вечеря" },
                new Category { Id = 3, Name = "Десерти", Description = "Сладки изкушения" },
                new Category { Id = 4, Name = "Салати", Description = "Свежи салати" }
            );
        }
    }
}
