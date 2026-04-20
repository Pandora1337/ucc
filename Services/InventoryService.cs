using System.Text.Json;
using ucc.Models;
using ucc.Data;
using TG.Blazor.IndexedDB;

namespace ucc.Services;

public class InventoryService
{
    public event Action? OnItemsChange;
    public event Action<string>? OnItemRemoved;
    public event Action? OnRecipesChange;

    private Dictionary<string, Item> items = [];
    private List<Recipe> recipes = [];

    private IndexedDBManager DB;
    public InventoryService(IndexedDBManager db)
    {
        DB = db;
        _ = DBinit();
    }

    private async Task DBinit()
    {
        await DB.OpenDb();

        // Get all items
        List<Item> itemList = await DB.GetRecords<Item>(IndexedDB.Items);
        items = itemList.ToDictionary(x => x.Id);
        OnItemsChange?.Invoke();

        // Get all recipes
        recipes = await DB.GetRecords<Recipe>(IndexedDB.Recipes);
        OnRecipesChange?.Invoke();

        if (items.Count == 0 && recipes.Count == 0)
        {
            GenerateStuff();
        }
    }

    void GenerateStuff()
    {
        TryAddItem("Crafting Table");
        TryAddItem("Log");
        TryAddItem("Plank");

        AddRecipe(new Recipe
        {
            ResultId = "plank",
            Yield = 4,
            Ingredients = [
                new("log", 1),
            ],
            BatchSize = 64,
        });

        AddRecipe(new Recipe
        {
            ResultId = "crafting-table",
            Yield = 1,
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

            OnItemsChange?.Invoke();
        }

        // Console.WriteLine($"{(resp ? "Added" : "Failed to add")} NEW ITEM: {newItem.Name} ID: {newItem.Id}");

        return resp;
    }

    public bool TryUpdateItem(string id, Item newItem)
    {
        bool resp = items.ContainsKey(id);
        if (resp)
        {
            items[id] = newItem;
            DB.UpdateRecord(new StoreRecord<Item>()
            {
                Storename = IndexedDB.Items,
                Data = newItem
            });

            OnItemsChange?.Invoke();
        }

        return resp;
    }

    public bool TryRemoveItem(string itemId)
    {
        if (RecipesWithItem(itemId).Count > 0)
        {
            Console.WriteLine($"Removing item ({itemId}) thats used in recipe(s)!");
        }

        bool resp = items.Remove(itemId);
        if (resp)
        {
            DB.DeleteRecord<string>(IndexedDB.Items, itemId);
            OnItemRemoved?.Invoke(itemId);
            OnItemsChange?.Invoke();
        }

        return resp;
    }

    public void ClearAllItems()
    {
        items.Clear();
        DB.ClearStore(IndexedDB.Items);
        OnItemsChange?.Invoke();
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
        return items.GetValueOrDefault(itemId)!;
    }

    public bool TryGetItem(string itemId, out Item item)
    {
        return items.TryGetValue(itemId, out item!);
    }

    public Dictionary<string, Item> GetItems()
    {
        return items;
    }

    public List<Recipe> RecipesWithItem(string itemId)
    {
        List<Recipe> list = new();
        foreach (Recipe recipe in recipes)
        {
            if (!recipe.ContainsItemId(itemId))
                continue;

            list.Add(recipe);
        }

        return list;
    }

    public void AddRecipe(Recipe recipe)
    {
        recipe.Guid = Guid.NewGuid();
        recipes.Add(recipe);
        DB.AddRecord(new StoreRecord<Recipe>()
        {
            Storename = IndexedDB.Recipes,
            Data = recipe
        });

        // Console.WriteLine($"{(resp ? "Added" : "Failed to add")} NEW Recipe for: {recipe.ResultId} ID: {2}");

        OnRecipesChange?.Invoke();
    }

    public bool TryUpdateRecipe(Guid guid, Recipe recipe)
    {
        int index = recipes.IndexOf(recipe);
        if (index < 0)
            return false;

        recipes[index] = recipe;
        DB.UpdateRecord(new StoreRecord<Recipe>()
        {
            Storename = IndexedDB.Recipes,
            Data = recipe
        });

        OnRecipesChange?.Invoke();
        return true;
    }

    public bool TryRemoveRecipe(Recipe recipe)
    {
        bool resp = recipes.Remove(recipe);
        if (resp)
        {
            DB.DeleteRecord(IndexedDB.Recipes, recipe.Guid);
            OnRecipesChange?.Invoke();
        }

        return resp;
    }

    public void ClearAllRecipes()
    {
        recipes.Clear();
        DB.ClearStore(IndexedDB.Recipes);
        OnRecipesChange?.Invoke();
    }

    // public Recipe GetRecipeById(string id)
    // {
    //     return recipes[id];
    // }

    // public Recipe? GetRecipeByResultId(string resultId)
    // {
    //     foreach (var i in recipes)
    //     {
    //         if (i.Value.ResultId == resultId)
    //         {
    //             return i.Value;
    //         }
    //     }
    //     return null;
    // }

    public List<Recipe> GetRecipes()
    {
        return recipes;
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
