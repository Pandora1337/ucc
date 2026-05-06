namespace ucc.Models;

public class CraftingData()
{
    public Dictionary<string, float> itemsProd = [];
    public Dictionary<string, float> itemsInt = [];
    public Dictionary<string, float> itemsRaw = [];
    public Dictionary<Recipe, int> recipeGuide = [];
    public float craftingTime = 0;
}