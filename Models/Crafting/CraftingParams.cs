namespace ucc.Models.Crafting;

public record CraftingParams()
{
    public Dictionary<string, float> Costs { get; set; } = [];
    public HashSet<Guid> Blacklist { get; set; } = [];
    public CraftingRate Rate { get; set; } = CraftingRate.PerSec;
}