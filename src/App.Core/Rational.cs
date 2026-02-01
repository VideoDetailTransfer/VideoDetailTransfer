using System.Globalization;

namespace VideoDetailTransfer.Core;

public readonly struct Rational : IEquatable<Rational>
{
    public long Numerator { get; }
    public long Denominator { get; }

    public static readonly Rational Zero = new Rational(0, 1);
    public static readonly Rational Invalid = new Rational(0, 0);

    public Rational(long numerator, long denominator)
    {
        if (denominator < 0)
        {
            numerator = -numerator;
            denominator = -denominator;
        }

        Numerator = numerator;
        Denominator = denominator;
    }

    public bool IsValid => Denominator != 0;

    public double ToDouble() => IsValid ? (double)Numerator / Denominator : double.NaN;

    public override string ToString() => IsValid ? $"{Numerator}/{Denominator}" : "0/0";

    public static Rational Parse(string? str)
    {
        if (string.IsNullOrWhiteSpace(str))
        {
            return Invalid;
        }

        str = str.Trim();

        // ffprobe uses "/" for frame rates, ":" for SAR/DAR sometimes
        char separator = str.Contains('/') ? '/' : (str.Contains(':') ? ':' : '\0');
        if (separator == '\0')
        {
            // Try plain integer or decimal
            if (long.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out long intValue))
            {
                return new Rational(intValue, 1);
            }

            if (double.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out double doubleValue))
            {
                // Not perfect, but good enough for odd cases; avoid for fps/SAR normally.
                const int scale = 1_000_000;
                return Reduce((long)Math.Round(doubleValue * scale), scale);
            }

            return Invalid;
        }

        string[] parts = str.Split(separator, 2);
        if (!long.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out long numerator))
        {
            return Invalid;
        }
        if (!long.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out long denominator))
        {
            return Invalid;
        }

        return Reduce(numerator, denominator);
    }

    public static Rational Reduce(long numerator, long denominator)
    {
        if (denominator == 0) return Invalid;
        if (numerator == 0) return Zero;

        long gcd = GreatestCommonDenominator(Math.Abs(numerator), denominator);
        return new Rational(numerator / gcd, denominator / gcd);
    }

    private static long GreatestCommonDenominator(long a, long b)
    {
        while (b != 0)
        {
            long t = a % b;
            a = b;
            b = t;
        }
        return a == 0 ? 1 : a;
    }

    public bool Equals(Rational other) => Numerator == other.Numerator && Denominator == other.Denominator;
    public override bool Equals(object? obj) => obj is Rational r && Equals(r);
    public override int GetHashCode() => HashCode.Combine(Numerator, Denominator);

    public static Rational operator *(Rational a, Rational b) => Reduce(a.Numerator * b.Numerator, a.Denominator * b.Denominator);

    public static Rational operator /(Rational a, Rational b) => Reduce(a.Numerator * b.Denominator, a.Denominator * b.Numerator);

    public Rational OrElse(Rational fallback) => IsValid ? this : fallback;
}
