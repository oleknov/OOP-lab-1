using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace SpreadsheetEditor.Core.Expressions;

public sealed record BinaryOperation(string Symbol, int Priority, bool RightAssociative,
    Func<BigInteger, BigInteger, BigInteger> Apply);

public sealed record UnaryOperation(string Symbol, Func<BigInteger, BigInteger> Apply);


public static class Operations
{
    private const int AddPriority = 1; 
    private const int MultiplyPriority = 2;  
    public const int PowerPriority = 3;

    private static readonly Dictionary<string, BinaryOperation> Binary = new BinaryOperation[]
    {
        new("+", AddPriority, false, (a, b) => a + b),
        new("-", AddPriority, false, (a, b) => a - b),
        new("*", MultiplyPriority, false, (a, b) => a * b),
        new("/", MultiplyPriority, false, Divide),
        new("^", PowerPriority, true, Power),
    }.ToDictionary(o => o.Symbol);

    private static readonly Dictionary<string, UnaryOperation> Prefix = new UnaryOperation[]
    {
        new("+", a => a),
        new("-", a => -a),
    }.ToDictionary(o => o.Symbol);

    private static readonly Dictionary<string, UnaryOperation> Functions = new UnaryOperation[]
    {
        new("inc", a => a + 1),
        new("dec", a => a - 1),
    }.ToDictionary(o => o.Symbol, StringComparer.OrdinalIgnoreCase);

    public static bool IsOperatorSymbol(char c) =>
        Binary.ContainsKey(c.ToString()) || Prefix.ContainsKey(c.ToString());

    public static bool TryGetBinary(string symbol, [NotNullWhen(true)] out BinaryOperation? operation) =>
        Binary.TryGetValue(symbol, out operation);

    public static bool TryGetPrefix(string symbol, [NotNullWhen(true)] out UnaryOperation? operation) =>
        Prefix.TryGetValue(symbol, out operation);

    public static bool TryGetFunction(string name, [NotNullWhen(true)] out UnaryOperation? operation) =>
        Functions.TryGetValue(name, out operation);

    private static BigInteger Divide(BigInteger a, BigInteger b) =>
        b.IsZero ? throw new ArithmeticException("ділення на нуль") : RoundHalfUp(a, b);

    private static BigInteger Power(BigInteger value, BigInteger exponent)
    {
        if (value.IsZero) return ZeroToPower(exponent);
        if (value.IsOne) return BigInteger.One;
        if (value == BigInteger.MinusOne) return exponent.IsEven ? BigInteger.One : BigInteger.MinusOne;

        return exponent.Sign < 0
            ? NegativePower(value, exponent)
            : PositivePower(value, exponent);
    }
    private static BigInteger ZeroToPower(BigInteger exponent)
    {
        if (exponent.Sign < 0) throw new ArithmeticException("ділення на нуль");
        return exponent.IsZero ? BigInteger.One : BigInteger.Zero;
    }
    private static BigInteger NegativePower(BigInteger value, BigInteger exponent)
    {
        if (BigInteger.Abs(value) > 2 || exponent < BigInteger.MinusOne) return BigInteger.Zero;
        return RoundHalfUp(BigInteger.One, value);
    }

    private static BigInteger RoundHalfUp(BigInteger p, BigInteger q)
    {
        if (q.Sign < 0) { p = -p; q = -q; } 
        BigInteger fraction = BigInteger.DivRem(2 * p + q, 2 * q, out var remainder);
        return remainder.Sign < 0 ? fraction - 1 : fraction; 
    }

    private static BigInteger PositivePower(BigInteger value, BigInteger exponent)
    {
        const string TooBig = "результат завеликий, щоб поміститися в пам'ять";

        if (exponent > int.MaxValue) throw new ArithmeticException(TooBig);

        try { return BigInteger.Pow(value, (int)exponent); }
        catch (Exception e) when (e is OverflowException or OutOfMemoryException)
        {
            throw new ArithmeticException(TooBig);
        }
    }
}