using ucc.Data;
using ucc.Models;

namespace ucc.Services;

public class CraftingService(InventoryService inventoryService, LocalStorage localStorage)
{
    protected InventoryService IS { get; set; } = inventoryService;
    protected LocalStorage LS { get; set; } = localStorage;

    public async Task InitializeAsync()
    {
        PlannedCrafts = await LS.Get<List<Ingredient>>("plannedCrafts", []);
        craftingData = await LS.Get<CraftingData>("craftingData", null);
    }

    public List<Ingredient> PlannedCrafts { get; set; } = new();

    public async Task OnItemDeleted(Ingredient ing)
    {
        PlannedCrafts.Remove(ing);
        await UpdatePlannedCrafts();
    }

    public async Task DeletePlannedCrafts()
    {
        PlannedCrafts.Clear();
        await LS.Remove("plannedCrafts");
    }

    public async Task UpdatePlannedCrafts()
    {
        await LS.Set("plannedCrafts", PlannedCrafts);
    }

    private CraftingData? craftingData = null;

    public CraftingData? GetCraftingData()
    {
        return craftingData;
    }

    public async Task SetCraftingData(CraftingData? newData)
    {
        craftingData = newData;
        await LS.Set("craftingData", craftingData!);
    }

    public async Task SetPlannedCrafts(List<Ingredient>? list)
    {
        PlannedCrafts = list ?? [];
        await UpdatePlannedCrafts();
    }

    public async Task DoListCrafting()
    {
        Dictionary<Recipe, int> recipeGuide = [];
        Dictionary<string, Recipe> selectedRecipes = [];

        Dictionary<string, float> totalIngs = [];
        Dictionary<string, float> itemDeltas = [];
        HashSet<string> deadEndItems = [];

        Dictionary<string, float> targetDict = CollapseList(PlannedCrafts);
        foreach (KeyValuePair<string, float> item in targetDict)
        {
            await Recurse(item.Key, item.Value, []);
        }

        CraftingData craftingData = new();
        foreach ((Recipe recipe, int ops) in recipeGuide)
        {
            craftingData.CraftingTime += recipe.GetTotalCraftingTime(ops);
            craftingData.RecipeGuide.Add(recipe.Guid, ops);
        }

        // Console.WriteLine("totalIngs:");
        // IS.SerialiseToJSON(totalIngs);

        // Console.WriteLine("deltas:");
        // IS.SerialiseToJSON(itemDeltas);

        SortItemCategories();
        await SetCraftingData(craftingData);

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
                List<Recipe> recipes = IS.GetRecipesByResultId(itemId).ToList();
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

            int ops = (int)Math.Ceiling(amount / totalProd[itemId]);
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
                            craftingData.ItemsInt.Add(itemId, totalIngs[itemId]);
                        }

                        craftingData.ItemsProd.Add(itemId, amount);
                        continue;

                    case 0:
                        craftingData.ItemsInt.Add(itemId, totalIngs[itemId]);
                        continue;

                    case < 0:
                        craftingData.ItemsRaw.Add(itemId, amount * -1);
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
    public IEnumerable<Recipe>? RecipeOptions { get; private set; }
    private async Task<Recipe> RequestUserResolve(string itemId, IEnumerable<Recipe> options)
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
