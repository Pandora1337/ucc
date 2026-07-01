using System.Text.RegularExpressions;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using ucc.Services;

namespace ucc.Models;

public partial class Item(string name) : IValidatableObject
{
    public static readonly string DefaultName = "New Item";

    public string Id { get; set; } = NameToId(name);

    [Required(ErrorMessage = "Name can't be empty!")]
    [StringLength(200, ErrorMessage = "Name is too long!")]
    public string Name { get; set; } = name;

    private string colorHex = "";
    public string ColorHex
    {
        get
        {
            return string.IsNullOrEmpty(colorHex) ? GetHashedColor() : colorHex;
        }
        set
        {
            colorHex = value;
        }
    }

    public DateTime DateModified { get; set; } = DateTime.Now;

    [JsonIgnore]
    public bool IsUnknown { get; private set; } = false;

    private string GetHashedColor()
    {
        int hash = this.GetHashCode();
        // Console.WriteLine(itemName + "'s hash: " + hash);
        byte r = (byte)((hash >> 0) & 0xFF);
        byte g = (byte)((hash >> 8) & 0xFF);
        byte b = (byte)((hash >> 16) & 0xFF);
        return $"#{r:X2}{g:X2}{b:X2}";
    }

    [GeneratedRegex(@"[^\p{L}\p{N}]+", RegexOptions.None)]
    private static partial Regex RegexSymbols();

    [GeneratedRegex(@"-+", RegexOptions.None)]
    private static partial Regex RegexDashes();

    // ID-ify Name
    public static string NameToId(string str)
    {
        string dashed = RegexSymbols().Replace(str.ToLowerInvariant(), "-");
        return RegexDashes().Replace(dashed.Trim('-'), "-");
    }

    public Item Copy()
    {
        return new Item(this.Name)
        {
            Id = this.Id,
            ColorHex = this.ColorHex,
            DateModified = this.DateModified,
        };
    }

    public static Item GetUnknown(string itemId)
    {
        return new(itemId)
        {
            IsUnknown = true,
            ColorHex = "#dc3545",
        };
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        InventoryService? IS = validationContext.GetService(typeof(InventoryService)) as InventoryService;
        if (IS?.ContainsItemId(NameToId(Name)) == true)
        {
            yield return new ValidationResult("Similar item already exists!",
                new[] { nameof(Name) });
        }
    }
}
