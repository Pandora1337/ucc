namespace ucc.Models.Crafting;

public class CraftingData()
{
    public Dictionary<string, float> ItemsProd { get; set; } = [];
    public Dictionary<string, float> ItemsInt { get; set; } = [];
    public Dictionary<string, float> ItemsRaw { get; set; } = [];
    // public Dictionary<Guid, int> RecipeGuide { get; set; } = [];
    public List<RecipeEntry> RecipeGuideList { get; set; } = [];
    public float CraftingTime { get; set; } = 0;
    public DateTime DateCrafted { get; set; } = DateTime.Now;
}

public sealed record RecipeEntry(Guid Guid, float Ops);