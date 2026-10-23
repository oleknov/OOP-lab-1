using System.Numerics;
using SpreadsheetEditor.Core.Expressions;
using IValueProvider = SpreadsheetEditor.Core.Expressions.IValueProvider;

namespace SpreadsheetEditor.Core;

public sealed class Cell
{
    private enum CalcState { Pending, InProgress, Done }

    private Node? tree;
    private ExpressionException? syntaxError;
    private CalcState state;
    private string? valueText;

    public string Expression { get; private set; } = "";
    public BigInteger Value { get; private set; }
    public ExpressionException? Error { get; private set; }
    public bool IsEmpty => string.IsNullOrWhiteSpace(Expression);

    private string ValueText => valueText ??= Value.ToString();

    internal bool IsCalculating => state == CalcState.InProgress;

    public string GetDisplayText(ViewMode mode) => mode switch
    {
        ViewMode.Expression => Expression,
        _ => Error is not null ? "#ПОМИЛКА" : IsEmpty ? "" : ValueText
    };

    internal void Define(string expression)
    {
        Expression = expression;
        tree = null;
        syntaxError = null;
        if (IsEmpty) return;
        try { tree = ExpressionParser.Parse(expression); }
        catch (ExpressionException e) { syntaxError = e; }
    }

    internal void Reset()
    {
        state = CalcState.Pending;
        Value = BigInteger.Zero;
        valueText = null;
        Error = null;
    }


    internal void Calculate(IValueProvider values)
    {
        if (state != CalcState.Pending) return;
        state = CalcState.InProgress;
        try
        {
            if (syntaxError is not null) throw syntaxError;
            Value = tree?.Evaluate(values) ?? BigInteger.Zero;
            valueText = null;
        }
        catch (ExpressionException e) { Error = e; }
        state = CalcState.Done;
    }
}
