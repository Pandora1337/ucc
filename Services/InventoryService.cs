using System.Text.Json;
using ucc.Models;

namespace ucc.Services;

public class InventoryService
{
    public event Action? OnItemsChange;
    public event Action<string>? OnItemRemoved;
    public event Action? OnRecipesChange;

    private Dictionary<string, Item> items = [];
    private List<Recipe> recipes = [];
    readonly JsonSerializerOptions jsonSerializerOptions = new()
    {
        WriteIndented = true
    };

    // Runs when singleton is created
    public InventoryService()
    {
        GenerateStuff();
    }

    void GenerateStuff()
    {
        TryAddItem("Crafting Table");
        TryAddItem("Log");
        TryAddItem("Plank");
        TryAddItem("ThIng/NEw           A-D+L[123](321)");

        Recipe r1 = new(
            GetItemIdByName("Plank"),
            4,
            [
                new("log", 1),
            ]
        );
        r1.PortionSize = 66;

        Recipe r2 = new(
            GetItemIdByName("Crafting Table"),
            1,
            [
                new("plank", 1),
                new("plank", 1),
                new("plank", 1),
                new("plank", 1),
            ],
            "crafting-table"
        );
        r2.PortionSize = 123;
        r2.CraftingTime = 33;

        // recipes.Add("111", r1);
        // recipes.Add("222", r2);
        recipes.Add(r1);
        recipes.Add(r2);
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

        // Console.WriteLine($"{(resp ? "Added" : "Failed to add")} NEW ITEM: {newItem.Name} ID: {newItem.Id}");

        if (resp)
        {
            OnItemsChange?.Invoke();
        }

        return resp;
    }

    public bool TryUpdateItem(string id, Item newItem)
    {
        bool resp = items.ContainsKey(id);
        if (resp)
        {
            items[id] = newItem;
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
            OnItemRemoved?.Invoke(itemId);
            OnItemsChange?.Invoke();
        }

        return resp;
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
        int index = recipes.IndexOf(recipe);
        if (index > -1)
        {
            recipes[index] = recipe;
        }
        else
        {
            recipes.Add(recipe);
        }

        // Console.WriteLine($"{(resp ? "Added" : "Failed to add")} NEW Recipe for: {recipe.ResultId} ID: {2}");

            OnRecipesChange?.Invoke();
    }
    
    public void RemoveRecipe(Recipe recipe)
    {
        recipes.Remove(recipe);

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
        string json = JsonSerializer.Serialize(data, jsonSerializerOptions);
        Console.WriteLine(json);
    }
}
