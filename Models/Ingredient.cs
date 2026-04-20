namespace ucc.Models;

public class Ingredient(
    string itemId = "",
    float amount = 1
    )
{
    public string ItemId { get; set; } = itemId;
    public float Amount { get; set; } = amount;
}