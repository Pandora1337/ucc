namespace ucc.Models.Solver;

public sealed class Simplex
{
    public record Result(
        Status Status,
        Dictionary<Guid, float> Solution,
        string ErrorMessage = ""
    );

    public enum Status
    {
        FAILURE,
        SUCCESS,
        TIMEOUT,
        CANCELLED,
    }

    public static async Task<Result> Solve(Matrix m, CancellationToken token)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        cts.CancelAfter(TimeSpan.FromSeconds(3));

        try
        {
            await SolveMatrix(m, cts.Token);
            return new(Status.SUCCESS, m.GetSolution());
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        {
            return new(Status.TIMEOUT, [], "The solver timed out.");
        }
        catch (OperationCanceledException)
        {
            return new(Status.CANCELLED, [], "The operation was canceled.");
        }
        catch (Exception ex)
        {
            return new(Status.FAILURE, [], ex.Message);
        }
    }

    /// <summary>
    /// Solve the matrix in place
    /// </summary>
    public static async Task SolveMatrix(Matrix m, CancellationToken token)
    {
        int iterations = 0;
        while (true)
        {
            iterations++;
            if (iterations % 100 == 0)
            {
                // yields for ui and checks token
                await Task.Delay(1, token);
            }

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

            // If min is not less than zero, we're done.
            if (min is not null && min >= Fraction.Zero)
            {
                return;
            }

            int? bestRow = PivotCol(m, minCol!.Value);
            if (!bestRow.HasValue)
            {
                throw new InvalidOperationException("Solver failed to pivot.");
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

        if (lowestVal is not null && lowestRow != null)
        {
            Pivot(m, lowestRow.Value, col);
        }

        return lowestRow;
    }

    public static void Pivot(Matrix m, int row, int col)
    {
        var x = m.ValueAt(row, col);
        m.MultiplyRow(row, x.Reciprocate());

        for (int r = 0; r < m.Rows; r++)
        {
            if (r == row) continue;

            var v = m.ValueAt(r, col);
            if (v == Fraction.Zero) continue;

            for (int c = 0; c < m.Cols; c++)
            {
                var local = m.ValueAt(r, c);
                var refValue = m.ValueAt(row, c);
                var newValue = local - (refValue * v);
                m.SetValue(r, c, newValue);
            }
        }
    }
}