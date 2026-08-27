using ucc.Models;

namespace ucc.Solver;

public sealed class Matrix
{
    public int Rows { get; private set; } = 0;
    public int Cols { get; private set; } = 0;

    private Fraction[,] _matrix;

    private Dictionary<string, int> _raws = [];
    private Dictionary<string, float> _costs = [];
    private List<string> _itemIndex = [];
    private List<Guid> _recipeIndex = [];

    #region Constructor
    public Matrix(List<Recipe> recipes, Dictionary<string, float> targets, Dictionary<string, float> costs)
    {
        var seen = new HashSet<string>();
        var raws = new HashSet<string>();
        foreach (Recipe recipe in recipes)
        {
            _recipeIndex.Add(recipe.Guid);
            foreach ((string ingId, float ingAmount) in recipe.Ingredients)
            {
                if (seen.Add(ingId))
                {
                    _itemIndex.Add(ingId);
                    raws.Add(ingId);
                }
            }

            foreach ((string prodId, float prodAmount) in recipe.Products)
            {
                if (seen.Add(prodId))
                {
                    _itemIndex.Add(prodId);
                }
                raws.Remove(prodId);
            }
        }

        foreach ((string itemId, float amount) in targets)
        {
            // if (amount >= 0) continue;
            if (seen.Add(itemId))
            {
                _itemIndex.Add(itemId);
            }
        }

        int itemCount = _itemIndex.Count;
        int recipeCount = recipes.Count;

        int rawCol = 0;
        foreach (string itemId in raws)
        {
            _raws[itemId] = itemCount + recipeCount + rawCol;
            if (!costs.ContainsKey(itemId))
            {
                costs[itemId] = 100;
                if (seen.Add(itemId))
                {
                    _itemIndex.Add(itemId);
                }
            }
            rawCol++;
        }

        // Console.WriteLine("COSTS:");
        // InventoryService.SerialiseToJSON(costs);

        // Console.WriteLine("RAWS:");
        // Serialise(raws);

        var itemIndex = new Dictionary<string, int>();
        for (int index = 0; index < _itemIndex.Count; index++)
        {
            itemIndex[_itemIndex[index]] = index;
        }

        // Tableau:
        //              [ings, prod] [recipes] [raws] [cost]
        // [ recipes  ]
        // [   raws   ]
        // [target out]


        // Cols = itemCount + recipeCount + 1;
        Cols = itemCount + recipeCount + costs.Count + 1;
        Rows = recipeCount + costs.Count + 1;
        _matrix = Fraction.Matrix(Rows, Cols, Fraction.Zero);

        int r = 0;
        foreach (Recipe recipe in recipes)
        {
            float time = recipe.CraftingTime ?? 1;

            // add ingredient columns
            foreach (var ing in recipe.Ingredients)
            {
                _matrix[r, itemIndex[ing.ItemId]] -= Fraction.FromDouble(ing.Amount / time);
            }

            // add products columns
            foreach (var prod in recipe.Products)
            {
                _matrix[r, itemIndex[prod.ItemId]] += Fraction.FromDouble(prod.Amount / time);
            }

            // set recipe-recipe value to 1
            _matrix[r, r + itemCount] = Fraction.One;

            // set cost of recipe to 1
            _matrix[r, Cols - 1] = Fraction.One;
            r++;
        }

        // add raw resource rows and their cost
        foreach ((string itemId, float cost) in costs)
        {
            if (!_itemIndex.Contains(itemId))
            {
                Cols -= 1;
                Rows -= 1;
                continue;
            }

            // set raw item input
            _matrix[r, itemIndex[itemId]] = Fraction.One;

            // set raw item delta
            _matrix[r, _raws[itemId]] = Fraction.One;

            // set cost
            _matrix[r, Cols - 1] = Fraction.FromDouble(cost);
            _costs[itemId] = cost;
            r++;
        }

        // add Output row
        foreach ((string itemId, float amount) in targets)
        {
            // if (amount >= 0) continue;
            _matrix[Rows - 1, itemIndex[itemId]] = Fraction.FromDouble(-float.Abs(amount));
        }
    }
    #endregion

    #region Other
    #endregion

    public Dictionary<Guid, float> GetSolution()
    {
        Dictionary<Guid, float> recipeGuide = [];
        int skipItems = _itemIndex.Count;
        for (int i = 0; i < _recipeIndex.Count; i++)
        {
            // OutputOfCol(i) _itemIndex
            Fraction val = OutputOfCol(skipItems + i);
            if (val == Fraction.Zero)
                continue;

            recipeGuide[_recipeIndex[i]] = (float)val.ToFloat();
            // recipeGuide[_recipeIndex[i]] = (int)Math.Ceiling(val.ToFloat());
        }

        return recipeGuide;
    }

    public Dictionary<string, int> GetRawDelta()
    {
        Console.WriteLine("RAW NEEDED:");
        foreach ((string itemId, int index) in _raws)
        {
            Console.WriteLine($"{itemId}: {OutputOfCol(index).ToFloat()}");
        }
        return _raws;
    }

    public Dictionary<string, float> GetCosts()
    {
        return _costs;
    }

    public void MultiplyRow(int row, Fraction value)
    {
        for (int col = 0; col < Cols; col++)
        {
            _matrix[row, col] *= value;
        }
    }

    public Fraction ValueAt(int row, int col)
    {
        return _matrix[row, col];
    }

    public void SetValue(int row, int col, Fraction value)
    {
        _matrix[row, col] = value;
    }

    public Fraction CostOfRow(int row)
    {
        return _matrix[row, Cols - 1];
    }

    public Fraction OutputOfCol(int col)
    {
        return _matrix[Rows - 1, col];
    }

    public void Print(bool isMixedFractions = false)
    {
        string items = string.Join(", ", _itemIndex);
        string recipes = string.Join(", ", _recipeIndex);
        string raws = string.Join(", ", _raws.Keys);

        Console.WriteLine($"Cols: {items}, {recipes}, {raws}, cost");
        Console.WriteLine($"Rows: {recipes}, {raws}, output");

        for (int i = 0; i < Rows; i++)
        {
            string row = "";
            for (int j = 0; j < Cols; j++)
            {
                if (isMixedFractions)
                {
                    row += _matrix[i, j].ToStringMixed() + " ";
                }
                else
                {
                    row += _matrix[i, j].ToString() + " ";
                }

            }
            Console.WriteLine(row);
        }
    }
}