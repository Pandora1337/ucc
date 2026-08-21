
using System.Numerics;

namespace ucc.Solver;

public sealed class Fraction
{
    public BigInteger Numerator { get; private set; } = BigInteger.Zero;
    public BigInteger Denominator { get; private set; } = BigInteger.One;

    public static readonly Fraction Zero = new(BigInteger.Zero, BigInteger.One);
    public static readonly Fraction One = new(BigInteger.One, BigInteger.One);

    public Fraction(BigInteger n, BigInteger d)
    {
        // keep D positive
        if (d < 0)
        {
            n = -n;
            d = -d;
        }

        BigInteger gcd = BigInteger.GreatestCommonDivisor(BigInteger.Abs(n), d);
        if (gcd > BigInteger.One)
        {
            n /= gcd;
            d /= gcd;
        }

        Numerator = n;
        Denominator = d;
    }

    public static Fraction FromDouble(double value, int maxDecimals = 10)
    {
        // Handle sign
        bool negative = value < 0;
        double abs = Math.Abs(value);

        // Separate integer and fractional parts
        long integerPart = (long)abs;
        double fracPart = abs - integerPart;

        if (fracPart == 0.0)
        {
            return new Fraction(negative ? -(int)integerPart : (int)integerPart, 1);
        }

        // Convert fractional part to string with limited precision
        string s = fracPart.ToString($"F{maxDecimals}", System.Globalization.CultureInfo.InvariantCulture);
        // Remove leading "0."
        int dotIndex = s.IndexOf('.');
        string digits = dotIndex >= 0 ? s.Substring(dotIndex + 1) : "0";

        // Remove trailing zeros
        digits = digits.TrimEnd('0');
        if (digits.Length == 0)
        {
            return new Fraction(negative ? -(int)integerPart : (int)integerPart, 1);
        }

        BigInteger numeratorFrac = long.Parse(digits);
        BigInteger denominatorFrac = (long)Math.Pow(10, digits.Length);

        // Reduce fraction by GCD
        BigInteger g = BigInteger.GreatestCommonDivisor(numeratorFrac, denominatorFrac);
        numeratorFrac /= g;
        denominatorFrac /= g;

        // Combine integer and fraction: integerPart + numeratorFrac/denominatorFrac
        BigInteger totalNumerator = integerPart * denominatorFrac + numeratorFrac;
        if (negative)
        {
            totalNumerator = -totalNumerator;
        }

        return new Fraction(totalNumerator, denominatorFrac);
    }

    public double ToFloat()
    {
        return (double)Numerator / (double)Denominator;
    }

    public static Fraction operator *(Fraction a, Fraction b)
    {
        BigInteger numerator = a.Numerator * b.Numerator;
        BigInteger denominator = a.Denominator * b.Denominator;
        return new Fraction(numerator, denominator);
    }

    public static Fraction operator /(Fraction a, Fraction b)
    {
        BigInteger numerator = a.Numerator * b.Denominator;
        BigInteger denominator = a.Denominator * b.Numerator;
        return new Fraction(numerator, denominator);
    }

    // Addition
    public static Fraction operator +(Fraction a, Fraction b)
    {
        // a/b + c/d = (ad + bc) / bd
        BigInteger numerator = a.Numerator * b.Denominator + b.Numerator * a.Denominator;
        BigInteger denominator = a.Denominator * b.Denominator;

        return new Fraction(numerator, denominator);
    }

    public static Fraction operator -(Fraction a, Fraction b)
    {
        // a/b - c/d = (ad - bc) / bd
        BigInteger numerator = a.Numerator * b.Denominator - b.Numerator * a.Denominator;
        BigInteger denominator = a.Denominator * b.Denominator;

        return new Fraction(numerator, denominator);
    }

    public static bool operator <(Fraction a, Fraction b)
    {
        return a.Numerator * b.Denominator < b.Numerator * a.Denominator;
    }

    public static bool operator >(Fraction a, Fraction b)
    {
        return a.Numerator * b.Denominator > b.Numerator * a.Denominator;
    }

    public static bool operator ==(Fraction a, Fraction b)
    {
        return a.Numerator * b.Denominator == b.Numerator * a.Denominator;
    }


    public static bool operator !=(Fraction a, Fraction b)
    {
        return !(a == b);
    }

    public static bool operator <=(Fraction a, Fraction b)
    {
        return a < b || a == b;
    }

    public static bool operator >=(Fraction a, Fraction b)
    {
        return a > b || a == b;
    }

    public Fraction Reciprocate()
    {
        return new Fraction(Denominator, Numerator);
    }

    public static Fraction[,] Matrix(int rows, int cols, Fraction initialValue = default!)
    {
        Fraction[,] data = new Fraction[rows, cols];
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                data[r, c] = initialValue;
            }
        }
        return data;
    }

    public override bool Equals(object? obj)
    {
        return obj is Fraction other && this == other;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Numerator, Denominator);
    }

    public override string ToString()
    {
        // if (this == Zero)
        //     return "0";
    
        return $"{Numerator}/{Denominator}";
    }

    public string ToStringMixed()
    {
        // if (this == Zero)
        //     return "0";
    
        BigInteger full = Numerator / Denominator;
        return $"{full}({Numerator - full * Denominator}/{Denominator})";
    }
}