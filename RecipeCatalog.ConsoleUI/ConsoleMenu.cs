using RecipeCatalog.Data.Models;
using RecipeCatalog.Services.Interfaces;

namespace RecipeCatalog.ConsoleUI
{
    public class ConsoleMenu
    {
        private readonly IRecipeService _recipeService;
        private readonly ICategoryService _categoryService;
        private readonly IReviewService _reviewService;

        public ConsoleMenu(IRecipeService recipeService, ICategoryService categoryService, IReviewService reviewService)
        {
            _recipeService = recipeService;
            _categoryService = categoryService;
            _reviewService = reviewService;

        }

        public async Task RunAsync()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("Каталог за рецепти");

            bool running = true;
            while (running)
            {
                Console.WriteLine("\n--- Главно меню ---");
                Console.WriteLine("1. Покажи всички рецепти");
                Console.WriteLine("2. Добави рецепта");
                Console.WriteLine("3. Редактирай рецепта");
                Console.WriteLine("4. Изтрий рецепта");
                Console.WriteLine("5. Търси рецепта");
                Console.WriteLine("6. Покажи по категория");
                Console.WriteLine("7. Управление на категории");
                Console.WriteLine("8. Ревюта за рецепта");
                Console.WriteLine("0. Изход");
                Console.Write("\nИзбор: ");

                var choice = Console.ReadLine();
                Console.WriteLine();

                switch (choice)
                {
                    case "1": await ShowAllRecipesAsync(); break;
                    case "2": await AddRecipeAsync(); break;
                    case "3": await EditRecipeAsync(); break;
                    case "4": await DeleteRecipeAsync(); break;
                    case "5": await SearchRecipesAsync(); break;
                    case "6": await ShowByCategoryAsync(); break;
                    case "7": await ManageCategoriesAsync(); break;
                    case "8": await ManageReviewsAsync(); break;
                    case "0": running = false; Console.WriteLine("Довиждане!"); break;
                    default: Console.WriteLine("Невалиден избор."); break;
                }
            }
        }

        private async Task ShowAllRecipesAsync()
        {
            var recipes = await _recipeService.GetAllAsync();
            var list = recipes.ToList();

            if (!list.Any())
            {
                Console.WriteLine("Няма добавени рецепти.");
                return;
            }

            Console.WriteLine($"{"ID",-5} {"Заглавие",-30} {"Категория",-20} {"Порции",-8} {"Общо време",-12}");
            Console.WriteLine(new string('-', 75));
            foreach (var r in list)
            {
                int totalTime = r.PreparationTimeMinutes + r.CookingTimeMinutes;
                Console.WriteLine($"{r.Id,-5} {r.Title,-30} {r.Category?.Name,-20} {r.Servings,-8} {totalTime} мин.");
            }
        }

        private async Task AddRecipeAsync()
        {
            Console.WriteLine("--- Добавяне на рецепта ---");

            var recipe = new Recipe();
            recipe.Title = ReadRequired("Заглавие: ");
            recipe.Description = ReadOptional("Описание (по желание): ");
            recipe.Instructions = ReadRequired("Инструкции: ");
            recipe.PreparationTimeMinutes = ReadInt("Време за подготовка (мин): ");
            recipe.CookingTimeMinutes = ReadInt("Време за готвене (мин): ");
            recipe.Servings = ReadInt("Брой порции: ");
            recipe.CategoryId = await SelectCategoryAsync();
            

            Console.WriteLine("Добавяне на съставки (оставете празно за да спрете):");
            while (true)
            {
                var name = ReadOptional("  Съставка: ");
                if (string.IsNullOrWhiteSpace(name)) break;

                var qty = ReadOptional("  Количество: ");
                var unit = ReadOptional("  Мерна единица: ");
                recipe.Ingredients.Add(new Ingredient { Name = name, Quantity = qty, Unit = unit });
            }

            await _recipeService.CreateAsync(recipe);
            Console.WriteLine("Рецептата е добавена успешно!");
        }

        private async Task EditRecipeAsync()
        {
            Console.Write("Въведете ID на рецептата: ");
            if (!int.TryParse(Console.ReadLine(), out int id)) return;

            var recipe = await _recipeService.GetByIdAsync(id);
            if (recipe == null)
            {
                Console.WriteLine("Рецептата не е намерена.");
                return;
            }

            Console.WriteLine($"Редактиране на: {recipe.Title}");
            Console.WriteLine("(Оставете празно за да запазите текущата стойност)");

            var title = ReadOptional($"Заглавие [{recipe.Title}]: ");
            if (!string.IsNullOrWhiteSpace(title)) recipe.Title = title;

            var desc = ReadOptional($"Описание [{recipe.Description}]: ");
            if (!string.IsNullOrWhiteSpace(desc)) recipe.Description = desc;

            var instructions = ReadOptional("Инструкции (Enter за пропускане): ");
            if (!string.IsNullOrWhiteSpace(instructions)) recipe.Instructions = instructions;

            await _recipeService.UpdateAsync(recipe);
            Console.WriteLine("Рецептата е обновена!");
        }

        private async Task DeleteRecipeAsync()
        {
            Console.Write("Въведете ID на рецептата за изтриване: ");
            if (!int.TryParse(Console.ReadLine(), out int id)) return;

            var recipe = await _recipeService.GetByIdAsync(id);
            if (recipe == null) { Console.WriteLine("Не е намерена."); return; }

            Console.Write($"Сигурни ли сте, че искате да изтриете '{recipe.Title}'? (д/н): ");
            if (Console.ReadLine()?.ToLower() == "д")
            {
                await _recipeService.DeleteAsync(id);
                Console.WriteLine("Изтрита успешно.");
            }
        }

        private async Task SearchRecipesAsync()
        {
            Console.Write("Търсене по ключова дума: ");
            var keyword = Console.ReadLine()?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(keyword))
            {
                Console.WriteLine("Не е въведена ключова дума.");
                return;
            }

            List<RecipeCatalog.Data.Models.Recipe> list;
            try
            {
                var results = await _recipeService.SearchAsync(keyword);
                list = results.ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Грешка при търсене: {ex.Message}");
                return;
            }

            if (!list.Any())
            {
                Console.WriteLine($"Няма намерени рецепти за {keyword}.");
                return;
            }

            Console.WriteLine($"\nНамерени {list.Count} рецепти за {keyword}:");
            Console.WriteLine(new string('-', 60));
            foreach (var r in list)
            {
                int totalTime = r.PreparationTimeMinutes + r.CookingTimeMinutes;
                Console.WriteLine($"  [{r.Id}] {r.Title}");
                Console.WriteLine($"       Категория : {r.Category?.Name ?? "—"}");
                Console.WriteLine($"       Порции    : {r.Servings}  |  Общо време: {totalTime} мин.");
                if (!string.IsNullOrWhiteSpace(r.Description))
                    Console.WriteLine($"       Описание  : {r.Description}");
                Console.WriteLine();
            }
        }

        private async Task ShowByCategoryAsync()
        {
            int categoryId = await SelectCategoryAsync();
            var recipes = await _recipeService.GetByCategoryAsync(categoryId);

            foreach (var r in recipes)
            {
                Console.WriteLine($"  [{r.Id}] {r.Title} - {r.Servings} порции");
            }
        }

        private async Task ManageCategoriesAsync()
        {
            Console.WriteLine("1. Покажи категории  2. Добави категория  3. Изтрий категория");
            Console.Write("Избор: ");
            var choice = Console.ReadLine();

            if (choice == "1")
            {
                var cats = await _categoryService.GetAllAsync();
                foreach (var c in cats)
                    Console.WriteLine($"  [{c.Id}] {c.Name} - {c.Description}");
            }
            else if (choice == "2")
            {
                var cat = new Category
                {
                    Name = ReadRequired("Име: "),
                    Description = ReadOptional("Описание: ")
                };
                await _categoryService.CreateAsync(cat);
                Console.WriteLine("Категорията е добавена!");
            }
            else if (choice == "3")
            {
                Console.Write("ID за изтриване: ");
                if (int.TryParse(Console.ReadLine(), out int id))
                {
                    bool deleted = await _categoryService.DeleteAsync(id);
                    Console.WriteLine(deleted ? "Изтрита." : "Не е намерена.");
                }
            }
        }

        private async Task ManageReviewsAsync()
        {
            Console.WriteLine("1. Покажи ревюта  2. Добави ревю  3. Изтрий ревю");
            Console.Write("Избор: ");
            switch (Console.ReadLine())
            {
                case "1":
                    Console.Write("ID на рецепта: ");
                    if (!int.TryParse(Console.ReadLine(), out int rId1)) break;
                    var reviews = (await _reviewService.GetByRecipeAsync(rId1)).ToList();
                    double avg = await _reviewService.GetAverageRatingAsync(rId1);
                    Console.WriteLine($"Средна оценка: {avg:F1}/5 ({reviews.Count} ревюта)");
                    foreach (var rv in reviews)
                        Console.WriteLine($"  [{rv.Id}] {rv.ReviewerName} — {rv.Rating}/5: {rv.Comment}");
                    break;
                case "2":
                    Console.Write("ID на рецепта: ");
                    int.TryParse(Console.ReadLine(), out int rId2);
                    var review = new Review
                    {
                        RecipeId = rId2,
                        ReviewerName = ReadRequired("Вашето име: "),
                        Rating = ReadInt("Оценка (1-5): "),
                        Comment = ReadOptional("Коментар: ")
                    };
                    await _reviewService.CreateAsync(review);
                    Console.WriteLine("Ревюто е добавено!");
                    break;
                case "3":
                    Console.Write("ID на ревю: ");
                    if (int.TryParse(Console.ReadLine(), out int rvId))
                        Console.WriteLine(await _reviewService.DeleteAsync(rvId) ? "Изтрито." : "Не е намерено.");
                    break;
            }
        }

        private async Task<int> SelectCategoryAsync()
        {
            var categories = (await _categoryService.GetAllAsync()).ToList();
            Console.WriteLine("Категории:");
            foreach (var c in categories)
                Console.WriteLine($"  {c.Id}. {c.Name}");
            Console.Write("Изберете категория (ID): ");
            int.TryParse(Console.ReadLine(), out int id);
            return id;
        }

        private static string ReadRequired(string prompt)
        {
            string? value;
            do
            {
                Console.Write(prompt);
                value = Console.ReadLine();
            } while (string.IsNullOrWhiteSpace(value));
            return value;
        }

        private static string ReadOptional(string prompt)
        {
            Console.Write(prompt);
            return Console.ReadLine() ?? "";
        }

        private static int ReadInt(string prompt)
        {
            Console.Write(prompt);
            int.TryParse(Console.ReadLine(), out int value);
            return value;
        }
    }
}
