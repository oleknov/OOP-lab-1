using System.Numerics;

namespace SpreadsheetEditor.Core.Expressions;

public interface IValueProvider
{
    BigInteger ValueOf(CellAddress address, int position);
}

public abstract class Node
{
    public abstract BigInteger Evaluate(IValueProvider values);

    protected static BigInteger Guard(int position, Func<BigInteger> operation)
    {
        try { return operation(); }
        catch (ArithmeticException e) { throw new ExpressionException(position, e.Message); }
    }
}

internal sealed class NumberNode(BigInteger value) : Node
{
    public override BigInteger Evaluate(IValueProvider values) => value;
}

internal sealed class ReferenceNode(CellAddress address, int position) : Node
{
    public override BigInteger Evaluate(IValueProvider values) => values.ValueOf(address, position);
}

internal sealed class UnaryNode(UnaryOperation operation, Node operand, int position) : Node
{
    public override BigInteger Evaluate(IValueProvider values)
    {
        var x = operand.Evaluate(values);
        return Guard(position, () => operation.Apply(x));
    }
}

internal sealed class BinaryNode(BinaryOperation operation, Node left, Node right, int position) : Node
{
    public override BigInteger Evaluate(IValueProvider values)
    {
        var a = left.Evaluate(values);
        var b = right.Evaluate(values);
        return Guard(position, () => operation.Apply(a, b));
    }
}
