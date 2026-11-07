namespace SpreadsheetEditor.Core.Tree;

public sealed class ExpressionException(int position, string description) : Exception(description)
{

    public int Position { get; } = position;

    public string Details => $"позиція {Position + 1}: {Message}";
}
