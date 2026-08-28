using ucc.Data;
using ucc.Models;
using ucc.Solver;

namespace ucc.Services;

public class CraftingService(InventoryService inventoryService, LocalStorage localStorage)
{
    protected InventoryService IS { get; set; } = inventoryService;
    protected LocalStorage LS { get; set; } = localStorage;

    public List<Ingredient> PlannedCrafts { get; set; } = new();
    public CraftingParams cp { get; private set; } = new();
    private CraftingData? craftingData = null;

    public async Task InitializeAsync()
    {
        PlannedCrafts = await LS.Get<List<Ingredient>>("plannedCrafts", []);
        craftingData = await LS.Get<CraftingData>("craftingData", null);
    }

    #region Craft
    public async Task Craft()
    {
        var graph = new Graph();
        var recipesList = new HashSet<Recipe>();

        Dictionary<string, float> targetDict = CollapseList(PlannedCrafts);
        foreach ((string itemId, float amount) in targetDict)
        {
            ExploreItem(itemId);
        }

        void ExploreItem(string itemId, Guid? parentRecipe = null)
        {
            var recipes = IS.GetRecipesByResultId(itemId);
            foreach (Recipe recipe in recipes)
            {
                if (graph.AddNode(recipe.Guid, parentRecipe))
                    continue;

                recipesList.Add(recipe);
                foreach ((string ingId, float ingAmount) in CollapseList(recipe.Ingredients))
                {
                    ExploreItem(ingId, recipe.Guid);
                }
            }
        }

        (var solution, cp.Costs) = Simplex.Solve(recipesList.ToList(), targetDict, cp.Costs);
        var cd = new CraftingData();

        // cumulative amount of ingredients used
        var ingCumulative = new Dictionary<string, float>();

        // overall change in item amounts
        var itemDeltas = new Dictionary<string, float>();

        // Console.WriteLine("Solution:");
        foreach (List<Guid> scc in graph.GetSCCs())
        {
            foreach (Guid guid in scc)
            {
                if (!solution.TryGetValue(guid, out float num))
                    continue;

                ApplyRecipeDeltas(guid, num, cd, itemDeltas, ingCumulative);
            }
        }

        // Console.WriteLine("ingCumulative:");
        // InventoryService.SerialiseToJSON(ingCumulative);

        // Console.WriteLine("deltas:");
        // InventoryService.SerialiseToJSON(itemDeltas);

        (cd.ItemsProd, cd.ItemsInt, cd.ItemsRaw) = SortItemCategories(itemDeltas, ingCumulative);
        await SetCraftingData(cd);
    }
    #endregion

    #region Recipe Deltas
    private void ApplyRecipeDeltas(Guid guid, float num, CraftingData cd, Dictionary<string, float> itemDeltas, Dictionary<string, float> ingCumulative)
    {
        var recipe = IS.GetRecipeById(guid);

        int ops = (int)Math.Ceiling(num);
        cd.RecipeGuideList.Add(new(guid, num));
        cd.CraftingTime += recipe.GetTotalCraftingTime(ops);

        // Console.WriteLine($"    {guid.ToString().Split("-")[0]}: {float.Round(num, 3)} ({ops})");

        float time = recipe.CraftingTime ?? 1;
        foreach ((string prodId, float prodAmount) in recipe.Products)
        {
            float prodMade = prodAmount * num / time;
            itemDeltas[prodId] = itemDeltas.GetValueOrDefault(prodId, 0) + prodMade;
        }

        foreach ((string ingId, float ingAmount) in recipe.Ingredients)
        {
            float ingNeed = ingAmount * num / time;
            ingCumulative[ingId] = ingCumulative.GetValueOrDefault(ingId, 0) + ingNeed;
            itemDeltas[ingId] = itemDeltas.GetValueOrDefault(ingId, 0) - ingNeed;
        }
    }
    #endregion

    #region Sort
    static (Dictionary<string, float>,
            Dictionary<string, float>,
            Dictionary<string, float>) SortItemCategories(Dictionary<string, float> itemDeltas, Dictionary<string, float> ingCumulative)
    {
        var prods = new Dictionary<string, float>();
        var inter = new Dictionary<string, float>();
        var raws = new Dictionary<string, float>();

        foreach ((string itemId, float amount) in itemDeltas)
        {
            switch (amount)
            {
                // Made more than needed - product
                case > 0:
                    // some part of product that is used as intermediate resource
                    if (ingCumulative.TryGetValue(itemId, out float need))
                    {
                        inter.Add(itemId, need);
                    }

                    if (WithinError(amount))
                        continue;

                    prods.Add(itemId, amount);
                    continue;

                // Made just enough - intermediate resource
                case 0:
                    inter.Add(itemId, ingCumulative[itemId]);
                    continue;

                // Didnt make enough - raw resource
                case < 0:
                    // some part of raw that is used as intermediate resource
                    // this really shouldnt be happening as all inters should've 
                    // been satisfied
                    // FIXME
                    if (ingCumulative.TryGetValue(itemId, out float has))
                    {
                        if (has != -amount)
                            inter.Add(itemId, has);
                    }

                    if (WithinError(amount))
                        continue;

                    raws.Add(itemId, -amount);
                    continue;
            }
        }

        static bool WithinError(float num)
        {
            float epsilon = 1e-5f;
            return float.Abs(num) < epsilon;
        }

        return (prods, inter, raws);
    }
    #endregion

    public static Dictionary<string, float> CollapseList(List<Ingredient> ingredients)
    {
        Dictionary<string, float> collapsed = [];
        foreach (Ingredient ing in ingredients)
        {
            collapsed[ing.ItemId] = collapsed.GetValueOrDefault(ing.ItemId, 0) + ing.Amount;
        }
        return collapsed;
    }

    #region Get/Set
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
    #endregion

    #region User Request
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
    #endregion
}
