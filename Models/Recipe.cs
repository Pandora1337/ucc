using System.ComponentModel.DataAnnotations;
using ucc.Services;

namespace ucc.Models;

public partial class Recipe(
    string resultId,
    int yield,
    List<Ingredient> ingredients,
    string stationId = "",
    int? portionSize = 0,
    int? craftingTime = 0
    ) : IValidatableObject
{
    public string ResultId { get; set; } = resultId;

    [Required(ErrorMessage = "Has to be more than 1!")]
    [Range(1, int.MaxValue)]
    public int Yield { get; set; } = yield;

    public List<Ingredient> Ingredients { get; set; } = ingredients;
    public string StationId { get; set; } = stationId;

    [Range(0, int.MaxValue)]
    public int? PortionSize { get; set; } = portionSize;

    [Range(0, int.MaxValue)]
    public int? CraftingTime { get; set; } = craftingTime;

    public DateTime CreatedAt { get; } = DateTime.Now;

    static public Recipe GetNew()
    {
        return new("", 1, []);
    }

    public bool ContainsItemId(string itemId)
    {
        if (ResultId == itemId)
        {
            return true;
        }

        if (StationId == itemId)
        {
            return true;
        }

        foreach (Ingredient ingr in Ingredients)
        {
            if (ingr.ItemId == itemId)
            {
                return true;
            }
        }

        return false;
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Ingredients.Count == 0)
        {
            yield return new ValidationResult("Need at least 1 ingredient!",
                new[] { nameof(Ingredients) });
        }

        InventoryService? IS = validationContext.GetService(typeof(InventoryService)) as InventoryService;

        if (!IS!.ContainsItemId(ResultId))
        {
            yield return new ValidationResult("The product item is invalid!",
                new[] { nameof(ResultId) });
        }

        if (!string.IsNullOrWhiteSpace(StationId) && !IS.ContainsItemId(StationId))
        {
            yield return new ValidationResult("The station item is invalid!",
                new[] { nameof(StationId) });
        }

        foreach (Ingredient ing in Ingredients)
        {
            if (!IS.ContainsItemId(ing.ItemId))
            {
                yield return new ValidationResult("One of the Ingredients is invalid!",
                new[] { nameof(Ingredients) });
            }
        }
    }
}
