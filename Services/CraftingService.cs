using ucc.Models;

namespace ucc.Services;

public class CraftingService(InventoryService inventoryService)
{
    protected InventoryService IS { get; set; } = inventoryService;

    public CraftingData? craftingData = null;
    public List<Ingredient> targetList = [];

    public event Action? OnCraftingFinish;

    public async Task DoListCrafting(List<Ingredient> products)
    {
        Dictionary<Recipe, int> recipeGuide = [];
        Dictionary<string, Recipe> selectedRecipes = [];

        Dictionary<string, float> totalIngs = [];
        Dictionary<string, float> itemDeltas = [];
        HashSet<string> deadEndItems = [];

        Dictionary<string, float> targetDict = CollapseList(products);
        foreach (KeyValuePair<string, float> item in targetDict)
        {
            await Recurse(item.Key, item.Value, []);
        }

        targetList = products;
        craftingData = new()
        {
            recipeGuide = recipeGuide,
        };

        foreach ((Recipe recipe, int ops) in recipeGuide)
        {
            craftingData.craftingTime += recipe.GetTotalCraftingTime(ops);
        }

        // Console.WriteLine("totalIngs:");
        // IS.SerialiseToJSON(totalIngs);

        // Console.WriteLine("deltas:");
        // IS.SerialiseToJSON(itemDeltas);

        SortItemCategories();

        OnCraftingFinish?.Invoke();

        async Task Recurse(string itemId, float amount, HashSet<string> visited)
        {
            if (deadEndItems.Contains(itemId))
                return;

            if (!visited.Add(itemId))
            {
                deadEndItems.Add(itemId);
                return;
            }

            if (!selectedRecipes.TryGetValue(itemId, out Recipe? recipe))
            {
                List<Recipe> recipes = IS.GetRecipesByResultId(itemId);
                if (recipes.Count == 0)
                {
                    deadEndItems.Add(itemId);
                    return;
                }

                if (recipes.Count == 1)
                {
                    recipe = recipes[0];
                }
                else
                {
                    recipe = await RequestUserResolve(itemId, recipes);
                    // TODO optional add to selectedRecipes?
                }

                selectedRecipes.Add(itemId, recipe);
            }

            Dictionary<string, float> totalProd = CollapseList(recipe.Products);

            int ops = (int) Math.Ceiling(amount / totalProd[itemId]);
            recipeGuide[recipe] = recipeGuide.GetValueOrDefault(recipe, 0) + ops;
            itemDeltas[itemId] = itemDeltas.GetValueOrDefault(itemId, 0) + (ops * totalProd[itemId]);

            Dictionary<string, float> totalIng = CollapseList(recipe.Ingredients);
            foreach ((string ingId, float ingAmount) in totalIng)
            {
                float ingNeed = ingAmount * ops;
                totalIngs[ingId] = totalIngs.GetValueOrDefault(ingId, 0) + ingNeed;

                float ingDelta = itemDeltas.GetValueOrDefault(ingId, 0) - ingNeed;
                itemDeltas[ingId] = ingDelta;
                if (ingDelta < 0)
                {
                    await Recurse(ingId, ingNeed, visited);
                }
            }

            visited.Remove(itemId);
        }

        void SortItemCategories()
        {
            foreach ((string itemId, float amount) in itemDeltas)
            {
                switch (amount)
                {
                    case > 0:
                        if (!targetDict.ContainsKey(itemId))
                        {
                            craftingData.itemsInt.Add(itemId, totalIngs[itemId]);
                        }

                        craftingData.itemsProd.Add(itemId, amount);
                        continue;

                    case 0:
                        craftingData.itemsInt.Add(itemId, totalIngs[itemId]);
                        continue;

                    case < 0:
                        craftingData.itemsRaw.Add(itemId, amount * -1);
                        continue;

                }
            }
        }
    }

    public static Dictionary<string, float> CollapseList(List<Ingredient> ingredients)
    {
        Dictionary<string, float> collapsed = [];
        foreach (Ingredient ing in ingredients)
        {
            collapsed[ing.ItemId] = collapsed.GetValueOrDefault(ing.ItemId, 0) + ing.Amount;
        }
        return collapsed;
    }

    public event Action<string>? OnChoiceRequest;
    public event Action? OnChoiceSelect;
    public List<Recipe>? RecipeOptions { get; private set; }
    private async Task<Recipe> RequestUserResolve(string itemId, List<Recipe> options)
    {
        RecipeOptions = options;
        _choiceTask = new();
        OnChoiceRequest?.Invoke(itemId);
        return await _choiceTask.Task;
    }

    private TaskCompletionSource<Recipe> _choiceTask = new();
    public void SelectRecipe(Recipe recipe)
    {
        RecipeOptions = null;
        OnChoiceSelect?.Invoke();
        _choiceTask.TrySetResult(recipe);
    }
}
