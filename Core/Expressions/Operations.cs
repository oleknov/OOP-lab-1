using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace SpreadsheetEditor.Core.Expressions;

public sealed record BinaryOperation(string Symbol, int Priority, bool RightAssociative,
    Func<BigInteger, BigInteger, BigInteger> Apply);

public sealed record UnaryOperation(string Symbol, Func<BigInteger, BigInteger> Apply);


public static class Operations
{
    public const int PowerPriority = 3;
    public const int MaxExponent = 10_000;

    private static readonly Dictionary<string, BinaryOperation> Binary = new BinaryOperation[]
    {
        new("+", 1, false, (a, b) => a + b),
        new("-", 1, false, (a, b) => a - b),
        new("*", 2, false, (a, b) => a * b),
        new("/", 2, false, Divide),
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
        b.IsZero ? throw new ArithmeticException("ділення на нуль") : BigInteger.Divide(a, b);

    private static BigInteger Power(BigInteger value, BigInteger exponent)
    {
        if (exponent.Sign < 0)
            throw new ArithmeticException("від'ємний показник степеня");
        if (exponent > MaxExponent)
            throw new ArithmeticException($"показник степеня більший за {MaxExponent}");
        return BigInteger.Pow(value, (int)exponent);
    }
}
