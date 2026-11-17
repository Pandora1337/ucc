using ucc.Models.Solver;

namespace ucc.Models.Crafting;

public class CraftingData()
{
    public Dictionary<string, float> ItemsProd { get; set; } = [];
    public Dictionary<string, float> ItemsInt { get; set; } = [];
    public Dictionary<string, float> ItemsRaw { get; set; } = [];
    public List<RecipeEntry> RecipeGuideList { get; set; } = [];
    public float CraftingTime { get; set; } = 0;
    public DateTime DateCrafted { get; set; } = DateTime.Now;

    public Simplex.Status Status { get; set; } = Simplex.Status.FAILURE;
    public string ErrorMessage { get; set; } = "";
}

public sealed record RecipeEntry(Guid Guid, float Ops);