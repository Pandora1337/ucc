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
    }

    public void GenerateStuff()
    {
        TryAddItem("Crafting Table");
        TryAddItem("Log");
        TryAddItem("Plank");

        AddRecipe(new Recipe
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

        AddRecipe(new Recipe
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

    private Dictionary<string, Item> items = [];

    public event Action? OnItemListChange;
    public event Action<string>? OnItemUpdate;

    public bool TryAddItem(string name)
    {
        if (name == "")
        {
            name = Item.DefaultName;
        }

        Item newItem = new(name);
        return TryAddItem(newItem);
    }

    public bool TryAddItem(Item newItem)
    {
        bool resp = items.TryAdd(newItem.Id, newItem);
        if (resp)
        {
            DB.AddRecord(new StoreRecord<Item>()
            {
                Storename = IndexedDB.Items,
                Data = newItem
            });

            OnItemListChange?.Invoke();
        }

        // Console.WriteLine($"{(resp ? "Added" : "Failed to add")} NEW ITEM: {newItem.Name} ID: {newItem.Id}");

        return resp;
    }

    public bool TryUpdateItem(string id, Item newItem)
    {
        if (!items.ContainsKey(id))
            return false;

        newItem.DateModified = DateTime.Now;
        items[id] = newItem;
        DB.UpdateRecord(new StoreRecord<Item>()
        {
            Storename = IndexedDB.Items,
            Data = newItem
        });

        OnItemUpdate?.Invoke(id);
        return true;
    }

    public bool TryRemoveItem(string itemId)
    {
        bool resp = items.Remove(itemId);
        if (resp)
        {
            DB.DeleteRecord<string>(IndexedDB.Items, itemId);
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

    public string GetItemIdByName(string itemName)
    {
        return items.FirstOrDefault(x => x.Value.Name == itemName).Key;
    }

    public string GetItemNameById(string itemId)
    {
        return items.TryGetValue(itemId, out Item? item) ? item.Name : "???";
    }

    public Item GetItem(string itemId)
    {
        return items.GetValueOrDefault(itemId, Item.GetUnknown(itemId));
    }

    public bool TryGetItem(string itemId, out Item item)
    {
        return items.TryGetValue(itemId, out item!);
    }

    public async Task SetItems(Dictionary<string, Item> newItems)
    {
        await DB.ClearStore(IndexedDB.Items);
        foreach ((string id, Item item) in newItems)
        {
            await DB.AddRecord<Item>(new()
            {
                Storename = IndexedDB.Items,
                Data = item
            });
        }

        items = newItems;
        OnItemListChange?.Invoke();
    }

    public Dictionary<string, Item> GetItems()
    {
        return items;
    }

    private Dictionary<Guid, Recipe> recipes = [];

    public event Action? OnRecipeListChange;
    public event Action<Guid>? OnRecipeUpdate;

    public void AddRecipe(Recipe recipe)
    {
        recipe.Guid = Guid.NewGuid();
        recipes.Add(recipe.Guid, recipe);
        DB.AddRecord(new StoreRecord<Recipe>()
        {
            Storename = IndexedDB.Recipes,
            Data = recipe
        });

        // Console.WriteLine($"{(resp ? "Added" : "Failed to add")} NEW Recipe for: {recipe.ResultId} ID: {2}");

        OnRecipeListChange?.Invoke();
    }

    public bool TryUpdateRecipe(Guid guid, Recipe recipe)
    {
        if (!recipes.ContainsKey(guid))
            return false;

        recipe.DateModified = DateTime.Now;
        recipes[guid] = recipe;
        DB.UpdateRecord(new StoreRecord<Recipe>()
        {
            Storename = IndexedDB.Recipes,
            Data = recipe
        });

        OnRecipeUpdate?.Invoke(guid);
        return true;
    }

    public bool TryRemoveRecipe(Guid id)
    {
        bool resp = recipes.Remove(id);
        if (resp)
        {
            DB.DeleteRecord(IndexedDB.Recipes, id);
            OnRecipeListChange?.Invoke();
        }

        return resp;
    }

    public async Task ClearAllRecipes()
    {
        recipes.Clear();
        await DB.ClearStore(IndexedDB.Recipes);
        OnRecipeListChange?.Invoke();
    }

    public Recipe GetRecipeById(Guid guid)
    {
        return recipes.GetValueOrDefault(guid)!;
    }

    public List<Recipe> GetRecipesWithItem(string itemId)
    {
        List<Recipe> list = new();
        foreach (Recipe recipe in recipes.Values)
        {
            if (!recipe.ContainsItemId(itemId))
                continue;

            list.Add(recipe);
        }

        return list;
    }

    public List<Recipe> GetRecipesByResultId(string resultId)
    {
        List<Recipe> list = [];
        foreach (Recipe recipe in recipes.Values)
        {
            foreach (Ingredient prod in recipe.Products)
            {
                if (prod.ItemId != resultId)
                    continue;

                list.Add(recipe);
                break;
            }
        }
        return list;
    }

    public async Task SetRecipes(Dictionary<Guid, Recipe> newRecipes)
    {
        await DB.ClearStore(IndexedDB.Recipes);
        foreach ((Guid id, Recipe recipe) in newRecipes)
        {
            await DB.AddRecord<Recipe>(new()
            {
                Storename = IndexedDB.Recipes,
                Data = recipe
            });
        }

        recipes = newRecipes;
        OnRecipeListChange?.Invoke();
    }

    public Dictionary<Guid, Recipe> GetRecipes()
    {
        return recipes;
    }

    public async Task ClearDB()
    {
        await DB.DeleteDb(DB.DbName);
        recipes.Clear();
        items.Clear();
        OnItemListChange?.Invoke();
        OnRecipeListChange?.Invoke();
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
