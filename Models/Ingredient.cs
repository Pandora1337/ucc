namespace ucc.Models;

public class Ingredient(
    string itemId = "",
    float amount = 1
    )
{
    public string ItemId { get; set; } = itemId;
    public float Amount { get; set; } = amount;

    public Ingredient Copy()
    {
        return new(this.ItemId, this.Amount);
    }

    internal void Deconstruct(out string itemId, out float amount)
    {
        itemId = this.ItemId;
        amount = this.Amount;
    }
}