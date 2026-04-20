using System.ComponentModel.DataAnnotations;
using ucc.Services;

namespace ucc.Models;

public partial class Recipe : IValidatableObject
{
    public string ResultId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Has to be more than 0!")]
    [Range(0, float.MaxValue)]
    public float Yield { get; set; } = 1;

    public List<Ingredient> Ingredients { get; set; } = [];
    public string StationId { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int? BatchSize { get; set; }

    [Range(0, float.MaxValue)]
    public float? CraftingTime { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public Guid Guid { get; set; }

    static public Recipe GetNew()
    {
        return new Recipe{ };
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
