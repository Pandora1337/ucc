using ucc.Models;

namespace ucc.Solver;

public sealed class Simplex
{
    public static Dictionary<Guid, float> Solve(List<Recipe> recipes, Dictionary<string, float> targets, Dictionary<string, float> costs)
    {
        var m = new Matrix(recipes, targets, costs);
        SolveMatrix(m);
        return m.GetSolution();
    }

    private static void SolveMatrix(Matrix m)
    {
        while (true)
        {
            Fraction? min = null;
            int? minCol = null;

            for (int col = 0; col < m.Cols - 1; col++)
            {
                // var x = m.ValueAt(m.Rows - 1, col);
                var x = m.OutputOfCol(col);
                if (min is not null && x >= min) continue;

                min = x;
                minCol = col;
            }

            //WriteLine($"min: {min}");
            //WriteLine($"col: {minCol}");

            // If min is not less than zero, we're done.
            if (min is not null && min >= Fraction.Zero)
            {
                return;
            }

            int? bestRow = PivotCol(m, minCol!.Value);
            if (!bestRow.HasValue)
            {
                // throw new InvalidOperationException("Failed to pivot.");
                Console.WriteLine("Failed to pivot!");
                return;
            }
        }
    }

    private static int? PivotCol(Matrix m, int col)
    {
        Fraction? lowestVal = null;
        int? lowestRow = null;
        for (int row = 0; row < m.Rows - 1; row++)
        {
            var x = m.ValueAt(row, col);
            if (Fraction.Zero >= x) continue;

            var ratio = m.CostOfRow(row) / x;

            if (lowestVal is not null && ratio >= lowestVal) continue;

            lowestVal = ratio;
            lowestRow = row;
        }

        //WriteLine($"ratio: {lowestVal}");
        //WriteLine($"row: {lowestRow}");


        // if (lowestVal != null && lowestRow != null)
        if (lowestVal is not null && lowestRow != null)
        {
            Pivot(m, lowestRow.Value, col);

            //WriteLine("");
            //WriteLine("PIVOT COMPLETE");
            // m.Print();
        }

        //WriteLine($"Lowest Row: {lowestRow}");

        return lowestRow;
    }

    public static void Pivot(Matrix m, int row, int col)
    {
        var x = m.ValueAt(row, col);

        //WriteLine($"Value at {row}, {col}: {x}");

        m.MultiplyRow(row, x.Reciprocate());
        //WriteLine("");
        //WriteLine($"After Divide by {x}");
        // m.Print();

        for (int r = 0; r < m.Rows; r++)
        {
            //WriteLine();
            //WriteLine($"Row: {r}");
            if (r == row) continue;

            var v = m.ValueAt(r, col);
            //WriteLine($"value: {v}");

            if (v == Fraction.Zero) continue;

            for (int c = 0; c < m.Cols; c++)
            {
                var local = m.ValueAt(r, c);
                var refValue = m.ValueAt(row, c);

                var newValue = local - (refValue * v);

                //WriteLine();
                //WriteLine($"Col: {c}");
                //WriteLine($"local: {localValue}");
                //WriteLine($"v: {v}");
                //WriteLine($"ref: {refValue}");
                //WriteLine($"new: {newValue}");
                m.SetValue(r, c, newValue);
            }
        }
    }
}