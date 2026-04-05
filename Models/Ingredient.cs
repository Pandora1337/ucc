namespace ucc.Models;

public class Ingredient(
    string itemId = "",
    int amount = 1
    )
{
    public string ItemId = itemId;
    public int Amount = amount;
}