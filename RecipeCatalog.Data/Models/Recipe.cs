using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecipeCatalog.Data.Models
{
    public class Recipe
    {
        public int Id { get; set; }
        [Required]

        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(5000)]
        public string Instructions { get; set; } = string.Empty;

        public int PreparationTimeMinutes { get; set; }

        public int CookingTimeMinutes { get; set; }

        public int Servings { get; set; }

        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;

        public ICollection<Ingredient> Ingredients { get; set; } = new List<Ingredient>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}
