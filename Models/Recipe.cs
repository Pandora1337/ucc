using System.ComponentModel.DataAnnotations;
using ucc.Services;

namespace ucc.Models;

public partial class Recipe : IValidatableObject
{
    public List<Ingredient> Products { get; set; } = [];

    public List<Ingredient> Ingredients { get; set; } = [];
    public string StationId { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int? BatchSize { get; set; }

    [Range(0, float.MaxValue)]
    public float? CraftingTime { get; set; }

    public DateTime DateModified { get; set; } = DateTime.Now;

    public Guid Guid { get; set; }

    static public Recipe GetNew()
    {
        return new Recipe { };
    }

    public Recipe Copy()
    {
        return new Recipe
        {
            Products = this.Products.Select(x => x.Copy()).ToList(),
            Ingredients = this.Ingredients.Select(x => x.Copy()).ToList(),
            StationId = this.StationId,
            BatchSize = this.BatchSize,
            CraftingTime = this.CraftingTime,
            DateModified = this.DateModified,
            Guid = this.Guid,
        };
    }

    public bool ContainsItemId(string itemId)
    {
        foreach (Ingredient prod in Products)
        {
            if (prod.ItemId == itemId)
            {
                return true;
            }
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

    /// <summary>
    /// Calculates the total crafting time of the recipe for int number of operations, taking into account BatchSize
    /// </summary>
    /// <param name="operations"></param>
    /// <returns>float seconds</returns>
    public float GetTotalCraftingTime(int operations)
    {
        if (!CraftingTime.HasValue || CraftingTime == 0)
        {
            return 0;
        }

        if (BatchSize > 0)
        {
            return (float)CraftingTime * float.Ceiling(operations / (float)BatchSize);
        }
        else
        {
            return (float)CraftingTime * operations;
        }
    }

    /// <summary>
    /// Compares the recipe's last modified date to a new DateTime value
    /// </summary>
    /// <param name="newDateTime"></param>
    /// <returns>
    /// True, if the value is later than recipe's. False if earlier or the same
    /// </returns>
    public bool IsOutdated(DateTime newDateTime)
    {
        return this.DateModified.CompareTo(newDateTime) < 0;
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Products.Count == 0)
        {
            yield return new ValidationResult("Need at least 1 Product!",
                new[] { nameof(Products) });
        }

        if (Ingredients.Count == 0)
        {
            yield return new ValidationResult("Need at least 1 Ingredient!",
                new[] { nameof(Ingredients) });
        }

        InventoryService? IS = validationContext.GetService(typeof(InventoryService)) as InventoryService;

        foreach (Ingredient ing in Products)
        {
            if (ing.Amount == 0)
            {
                yield return new ValidationResult("One of the Product amounts is 0!",
                new[] { nameof(Products) });
            }

            if (!IS!.ContainsItemId(ing.ItemId))
            {
                yield return new ValidationResult("One of the Products is invalid!",
                new[] { nameof(Ingredients) });
            }
        }

        if (!string.IsNullOrWhiteSpace(StationId) && !IS!.ContainsItemId(StationId))
        {
            yield return new ValidationResult("The station item is invalid!",
                new[] { nameof(StationId) });
        }

        foreach (Ingredient ing in Ingredients)
        {
            if (ing.Amount == 0)
            {
                yield return new ValidationResult("One of the Ingredient amounts is 0!",
                new[] { nameof(Ingredients) });
            }

            if (!IS!.ContainsItemId(ing.ItemId))
            {
                yield return new ValidationResult("One of the Ingredients is invalid!",
                new[] { nameof(Ingredients) });
            }
        }
    }
}
