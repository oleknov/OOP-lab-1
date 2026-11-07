using SpreadsheetEditor.Core.Parser;
using SpreadsheetEditor.Core.Tree;
using System.Globalization;
using System.Numerics;
using IValueProvider = SpreadsheetEditor.Core.Tree.IValueProvider;

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

    private string ValueText => valueText ??= Value.ToString(CultureInfo.InvariantCulture);

    internal bool IsCalculating => state == CalcState.InProgress;

    public string GetDisplayText(ViewMode mode) => mode switch
    {
        ViewMode.Expression => Expression,
        _ => Error is not null ? "#ПОМИЛКА" : IsEmpty ? "" : NumberFormatter.Compact(ValueText)
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

    public string FullValue => IsEmpty || Error is not null ? "" : ValueText;
}
