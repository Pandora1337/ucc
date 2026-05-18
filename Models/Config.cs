namespace ucc.Models;

public class Config
{
    public Dictionary<string, Item> Items { get; set; } = new();
    public Dictionary<Guid, Recipe> Recipes { get; set; } = new();
    public List<Ingredient>? PlannedCrafts { get; set; } = null;
    public CraftingData? CraftingData { get; set; } = null;
}