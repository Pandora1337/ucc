using System.Text.Json;
using ucc.Models;
using ucc.Data;
using TG.Blazor.IndexedDB;

namespace ucc.Services;

public class InventoryService(IndexedDBManager db)
{
    private IndexedDBManager DB = db;

    public async Task InitializeAsync()
    {
        await DB.OpenDb();

        // Get all items
        List<Item> itemList = await DB.GetRecords<Item>(IndexedDB.Items);
        items = itemList.ToDictionary(x => x.Id);
        OnItemListChange?.Invoke();

        // Get all recipes
        List<Recipe> recipeList = await DB.GetRecords<Recipe>(IndexedDB.Recipes);
        recipes = recipeList.ToDictionary(x => x.Guid);
        OnRecipeListChange?.Invoke();

        foreach (Recipe recipe in recipeList)
        {
            ChangeIndex(recipe);
        }
    }

    public async void GenerateStuff()
    {
        await TryAddItem("Crafting Table");
        await TryAddItem("Log");
        await TryAddItem("Plank");

        await TryAddRecipe(new Recipe
        {
            Products = [
              new("plank", 2),
              new("plank", 2),
            ],
            Ingredients = [
                new("log", 1),
            ],
            BatchSize = 16,
        });

        await TryAddRecipe(new Recipe
        {
            Products = [
              new("crafting-table", 1),
            ],
            Ingredients = [
                new("plank", 1),
                new("plank", 1),
                new("plank", 1),
                new("plank", 1),
            ],
            StationId = "crafting-table",
            BatchSize = 64,
        });
    }

    public Dictionary<string, HashSet<Guid>> itemToRecipes = [];
    private void ChangeIndex(Recipe? recipe, bool isRemove = false)
    {
        if (recipe == null)
            return;

        foreach (Ingredient prod in recipe.Products)
        {
            ChangeGuid(prod.ItemId);
        }

        foreach (Ingredient ingr in recipe.Ingredients)
        {
            ChangeGuid(ingr.ItemId);
        }

        if (!string.IsNullOrEmpty(recipe.StationId))
        {
            ChangeGuid(recipe.StationId);
        }

        void ChangeGuid(string itemId)
        {
            HashSet<Guid> guids = itemToRecipes.GetValueOrDefault(itemId, []);
            _ = isRemove ? guids.Remove(recipe.Guid) : guids.Add(recipe.Guid);
            itemToRecipes[itemId] = guids;
        }
    }

    #region Items

    private Dictionary<string, Item> items = [];

    public event Action? OnItemListChange;
    public event Action<string>? OnItemUpdate;

    public async Task<bool> TryAddItem(string name)
    {
        if (name == "")
        {
            name = Item.DefaultName;
        }

        Item newItem = new(name);
        return await TryAddItem(newItem);
    }

    public async Task<bool> TryAddItem(Item newItem)
    {
        if (string.IsNullOrEmpty(newItem.Id))
            return false;

        if (!items.TryAdd(newItem.Id, newItem))
            return false;

        await DB.AddRecord(new StoreRecord<Item>()
        {
            Storename = IndexedDB.Items,
            Data = newItem
        });

        OnItemListChange?.Invoke();
        OnItemUpdate?.Invoke(newItem.Id);

        // Console.WriteLine($"{(resp ? "Added" : "Failed to add")} NEW ITEM: {newItem.Name} ID: {newItem.Id}");

        return true;
    }

    public async Task<bool> TryUpdateItem(string id, Item newItem)
    {
        if (!items.ContainsKey(id))
            return false;

        newItem.DateModified = DateTime.Now;
        items[id] = newItem;
        await DB.UpdateRecord(new StoreRecord<Item>()
        {
            Storename = IndexedDB.Items,
            Data = newItem
        });

        OnItemUpdate?.Invoke(id);
        return true;
    }

    public async Task<bool> TryRemoveItem(string itemId)
    {
        bool resp = items.Remove(itemId);
        if (resp)
        {
            await DB.DeleteRecord<string>(IndexedDB.Items, itemId);
            OnItemUpdate?.Invoke(itemId);
            OnItemListChange?.Invoke();
        }

        return resp;
    }

    public async Task ClearAllItems()
    {
        items.Clear();
        await DB.ClearStore(IndexedDB.Items);
        OnItemListChange?.Invoke();
    }

    public bool ContainsItemId(string id)
    {
        return items.ContainsKey(id);
    }

    public Item GetItem(string itemId)
    {
        return items.GetValueOrDefault(itemId, Item.GetUnknown(itemId));
    }

    public async Task SetItems(Dictionary<string, Item> newItems)
    {
        await ClearAllItems();
        foreach ((string id, Item item) in newItems)
        {
            await TryAddItem(item);
        }
    }

    #region Search
    public IEnumerable<string> SearchItemIds(string search)
    {
        return SearchItems(search).Select(x => x.Id);
    }

    public IEnumerable<Item> SearchItems(string search)
    {
        IEnumerable<Item> itemObjs = items.Values;
        if (string.IsNullOrEmpty(search))
            return itemObjs;

        return itemObjs.Where(item => item.Name.Contains(search,
            StringComparison.OrdinalIgnoreCase)
        );
    }
    #endregion

    public Dictionary<string, Item> GetItems()
    {
        return items;
    }

    #region Sort
    public enum ItemSort
    {
        Name,
        Date,
    }

    public IEnumerable<string> DoItemIdSort(IEnumerable<string> toFilter, ItemSort itemSort, bool IsAscending = true)
    {
        IEnumerable<Item> items = toFilter.Select(GetItem);
        return DoItemIdSort(items, itemSort, IsAscending);
    }

    public IEnumerable<string> DoItemIdSort(IEnumerable<Item> toFilter, ItemSort itemSort, bool IsAscending = true)
    {
        IEnumerable<Item> filteredItems = itemSort switch
        {
            ItemSort.Name => SortByDirection(toFilter, x => x.Name, IsAscending),
            ItemSort.Date => SortByDirection(toFilter, x => x.DateModified, IsAscending),
            _ => toFilter
        };

        return filteredItems.Select(x => x.Id);
    }
    #endregion
    #endregion

    #region Recipes
    private Dictionary<Guid, Recipe> recipes = [];

    public event Action? OnRecipeListChange;
    public event Action<Guid>? OnRecipeUpdate;

    public async Task<bool> TryAddRecipe(Recipe recipe)
    {
        if (recipe.Guid == Guid.Empty)
            return false;

        if (!recipes.TryAdd(recipe.Guid, recipe))
            return false;

        await DB.AddRecord(new StoreRecord<Recipe>()
        {
            Storename = IndexedDB.Recipes,
            Data = recipe
        });

        ChangeIndex(recipe);
        // Console.WriteLine($"{(resp ? "Added" : "Failed to add")} NEW Recipe for: {recipe.ResultId} ID: {2}");

        OnRecipeListChange?.Invoke();
        OnRecipeUpdate?.Invoke(recipe.Guid);

        return true;
    }

    public async Task<bool> TryUpdateRecipe(Guid guid, Recipe recipe)
    {
        if (!recipes.ContainsKey(guid))
            return false;

        recipe.DateModified = DateTime.Now;
        recipes[guid] = recipe;
        await DB.UpdateRecord(new StoreRecord<Recipe>()
        {
            Storename = IndexedDB.Recipes,
            Data = recipe
        });

        ChangeIndex(recipe, true);
        ChangeIndex(recipe);
        OnRecipeUpdate?.Invoke(guid);
        return true;
    }

    public bool TryRemoveRecipe(Guid id)
    {
        bool resp = recipes.Remove(id, out Recipe? recipe);
        if (resp)
        {
            ChangeIndex(recipe, true);
            DB.DeleteRecord(IndexedDB.Recipes, id);
            OnRecipeListChange?.Invoke();
        }

        return resp;
    }

    public async Task ClearAllRecipes()
    {
        itemToRecipes.Clear();
        recipes.Clear();
        await DB.ClearStore(IndexedDB.Recipes);
        OnRecipeListChange?.Invoke();
    }

    public Recipe GetRecipeById(Guid guid)
    {
        return recipes.GetValueOrDefault(guid)!;
    }

    #region Get/Set
    public IEnumerable<Guid> GetRecipesWithItems(IEnumerable<string> items)
    {
        var guids = new HashSet<Guid>();
        foreach (string item in items)
        {
            foreach (Recipe recipe in GetRecipesWithItem(item))
            {
                if (guids.Add(recipe.Guid))
                    yield return recipe.Guid;
            }
        }
    }

    public IEnumerable<Recipe> GetRecipesWithItem(string itemId)
    {
        if (!itemToRecipes.TryGetValue(itemId, out HashSet<Guid>? guids))
            yield break;

        foreach (Guid guid in guids)
        {
            yield return GetRecipeById(guid);
        }
    }

    public IEnumerable<Recipe> GetRecipesByResultId(string resultId)
    {
        foreach (Recipe recipe in GetRecipesWithItem(resultId))
        {
            if (recipe.ContainsProductId(resultId))
            {
                yield return recipe;
            }
        }
    }

    public async Task SetRecipes(Dictionary<Guid, Recipe> newRecipes)
    {
        await ClearAllRecipes();
        foreach ((Guid id, Recipe recipe) in newRecipes)
        {
            await TryAddRecipe(recipe);
        }
    }

    public Dictionary<Guid, Recipe> GetRecipes()
    {
        return recipes;
    }
    #endregion

    #region Search
    public IEnumerable<Recipe> SearchRecipesByItemsAndName(string search)
    {
        var items = SearchItemIds(search);
        var byItem = GetRecipesWithItems(items).Select(GetRecipeById);
        var byName = SearchRecipesByName(search);
        return byName.Concat(byItem).DistinctBy(r => r.Guid);
    }

    public IEnumerable<Guid> SearchRecipeIdsByName(string search)
    {
        return SearchRecipesByName(search).Select(x => x.Guid);
    }

    public IEnumerable<Recipe> SearchRecipesByName(string search)
    {
        IEnumerable<Recipe> recipeValues = recipes.Values;
        if (string.IsNullOrEmpty(search))
            return recipeValues;

        return recipeValues.Where(recipe => recipe.Name.Contains(search,
            StringComparison.OrdinalIgnoreCase)
        );
    }
    #endregion

    #region Sort
    public enum RecipeSort
    {
        Name,
        Date,
        Station,
        BatchSize,
        CraftingTime,
    }

    public IEnumerable<Guid> DoRecipeIdSort(IEnumerable<Guid> toFilter, RecipeSort recipeSort, bool IsAscending = true)
    {
        IEnumerable<Recipe> recipes = toFilter.Select(GetRecipeById);
        return DoRecipeIdSort(recipes, recipeSort, IsAscending);
    }

    public IEnumerable<Guid> DoRecipeIdSort(IEnumerable<Recipe> toFilter, RecipeSort recipeSort, bool IsAscending = true)
    {
        IEnumerable<Recipe> filteredRecipes = recipeSort switch
        {
            RecipeSort.Name => SortByDirection(toFilter, x => x.Name, IsAscending),
            RecipeSort.Date => SortByDirection(toFilter, x => x.DateModified, IsAscending),
            RecipeSort.Station => SortByDirection(toFilter, x => x.StationId, IsAscending),
            RecipeSort.BatchSize => SortByDirection(toFilter, x => x.BatchSize, IsAscending),
            RecipeSort.CraftingTime => SortByDirection(toFilter, x => x.CraftingTime, IsAscending),
            _ => toFilter
        };

        return filteredRecipes.Select(x => x.Guid);
    }
    #endregion
    #endregion

    public async Task ClearDB()
    {
        await DB.DeleteDb(DB.DbName);
        recipes.Clear();
        items.Clear();
        OnItemListChange?.Invoke();
        OnRecipeListChange?.Invoke();
    }

    private static IOrderedEnumerable<T> SortByDirection<T, TKey>(IEnumerable<T> source, Func<T, TKey> selector, bool ascending)
    {
        return ascending ? source.OrderBy(selector) : source.OrderByDescending(selector);
    }

    public void SerialiseToJSON(object data)
    {
        string json = JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            WriteIndented = true,
        });
        Console.WriteLine(json);
    }
}
