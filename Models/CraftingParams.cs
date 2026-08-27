namespace ucc.Models;

public record CraftingParams()
{
    public Dictionary<string, float> Costs { get; set; } = [];
}