using ucc.Data;
using ucc.Models;
using ucc.Models.Crafting;
using ucc.Models.Solver;

namespace ucc.Services;

public class CraftingService(InventoryService inventoryService, LocalStorage localStorage)
{
    protected InventoryService IS { get; set; } = inventoryService;
    protected LocalStorage LS { get; set; } = localStorage;

    public List<Ingredient> PlannedCrafts { get; set; } = new();
    public CraftingParams CraftingParams { get; set; } = new();
    private CraftingData? craftingData = null;

    public async Task InitializeAsync()
    {
        PlannedCrafts = await LS.Get<List<Ingredient>>("plannedCrafts", []);
        CraftingParams = await LS.Get("craftingParams", CraftingParams);
        craftingData = await LS.Get<CraftingData>("craftingData", null);
    }

    #region Craft
    public async Task Craft()
    {
        var graph = new Graph();
        var recipesList = new HashSet<Recipe>();
        Dictionary<string, float> targetDict = [];
        foreach ((string itemId, float amount) in PlannedCrafts)
        {
            if (amount <= 0)
                continue;

            // dedup is done in Crafting.razor
            targetDict[itemId] = amount;
            ExploreItem(itemId);
        }

        void ExploreItem(string itemId, Guid? parentRecipe = null)
        {
            bool hasRecipe = false;
            var recipes = IS.GetRecipesByResultId(itemId);
            foreach (Recipe recipe in recipes)
            {
                if (CraftingParams.Blacklist.Contains(recipe.Guid))
                    continue;

                hasRecipe = true;
                // skip if already in the graph
                if (graph.AddNode(recipe.Guid, parentRecipe))
                    continue;

                recipesList.Add(recipe);
                foreach ((string ingId, float ingAmount) in recipe.Ingredients.DistinctBy(x => x.ItemId))
                {
                    ExploreItem(ingId, recipe.Guid);
                }
            }

            if (!hasRecipe)
            {
                targetDict.Remove(itemId);
            }
        }

        if (targetDict.Count == 0)
            return;

        var cd = new CraftingData();

        // cumulative amount of ingredients used
        var ingCumulative = new Dictionary<string, float>();

        // overall change in item amounts
        var itemDeltas = new Dictionary<string, float>();

        var answer = SolveRecipes(cd, recipesList.ToList(), targetDict, itemDeltas, ingCumulative);

        // Branch and run again for any deficits to get integer solution
        if (CraftingParams.Rate == CraftingRate.Once)
        {
            var targetDict2 = new Dictionary<string, float>();
            foreach ((string itemId, float amount) in itemDeltas)
            {
                float diff = targetDict.GetValueOrDefault(itemId, 0) - amount;
                if (diff <= 0)
                    continue;

                targetDict2[itemId] = diff;
            }

            // skip if no deficits
            if (targetDict2.Count > 0)
            {
                var answer2 = SolveRecipes(cd, recipesList.ToList(), targetDict2, itemDeltas, ingCumulative);
                foreach ((Guid guid, float amount) in answer2)
                {
                    // merge answers
                    answer[guid] = answer.GetValueOrDefault(guid, 0) + amount;
                }
            }
        }

        // topologically sorted recipe graph for recipe order
        foreach (Guid guid in graph.GetSCCs().SelectMany(scc => scc))
        {
            if (!answer.TryGetValue(guid, out float num))
                continue;

            cd.RecipeGuideList.Add(new(guid, num));
        }

        (cd.ItemsProd, cd.ItemsInt, cd.ItemsRaw) = SortItemCategories(itemDeltas, ingCumulative);
        await SetCraftingData(cd);
        await UpdateCraftingParams();
    }
    #endregion

    #region SolveRecipes
    private Dictionary<Guid, float> SolveRecipes(
        CraftingData cd,
        List<Recipe> recipes,
        Dictionary<string, float> targets,
        Dictionary<string, float> itemDeltas,
        Dictionary<string, float> ingCumulative)
    {
        bool normalise = CraftingParams.Rate != CraftingRate.Once;
        var m = new Matrix(recipes, targets, CraftingParams.Costs, normalise);
        (var solution, CraftingParams.Costs) = Simplex.Solve(m, CraftingParams.Costs);

        var output = new Dictionary<Guid, float>(solution.Count);
        foreach ((Guid guid, float ops) in solution)
        {
            float num = ops;
            var recipe = IS.GetRecipeById(guid);
            if (!normalise)
            {
                num = float.Ceiling(num);
                cd.CraftingTime += recipe.GetTotalCraftingTime((int)num);
            }

            output[guid] = num;
            float time = normalise ? (recipe.CraftingTime ?? 1) : 1;
            AddRecipeDeltas(recipe, num / time, itemDeltas, ingCumulative);
        }

        return output;
    }
    #endregion

    #region Recipe Deltas
    private static void AddRecipeDeltas(
        Recipe recipe,
        float num,
        Dictionary<string, float> itemDeltas,
        Dictionary<string, float> ingCumulative
    )
    {
        foreach ((string prodId, float prodAmount) in recipe.Products)
        {
            float prodMade = prodAmount * num;
            itemDeltas[prodId] = itemDeltas.GetValueOrDefault(prodId, 0) + prodMade;
        }

        foreach ((string ingId, float ingAmount) in recipe.Ingredients)
        {
            float ingNeed = ingAmount * num;
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

        // makes sure the order of matching items in prod follows inter
        var newprods = new Dictionary<string, float>();
        foreach (string item in inter.Keys)
        {
            if (prods.TryGetValue(item, out float value))
                newprods[item] = value;
        }

        foreach ((string item, float amount) in prods)
        {
            if (!newprods.ContainsKey(item))
                newprods[item] = amount;
        }

        return (newprods, inter, raws);
    }
    #endregion

    #region Get/Set
    public async Task OnItemDeleted(Ingredient ing)
    {
        PlannedCrafts.Remove(ing);
        await UpdatePlannedCrafts();
    }

    public async Task DeletePlannedCrafts()
    {
        PlannedCrafts.Clear();
        await UpdatePlannedCrafts();
    }

    public async Task UpdatePlannedCrafts()
    {
        await LS.Set("plannedCrafts", PlannedCrafts);
    }

    public async Task SetCraftingParams(CraftingParams? craftingParams)
    {
        CraftingParams = craftingParams ?? new();
        await UpdateCraftingParams();
    }

    public async Task UpdateCraftingParams()
    {
        await LS.Set("craftingParams", CraftingParams);
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
}
