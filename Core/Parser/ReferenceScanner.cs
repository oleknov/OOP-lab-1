using Antlr4.Runtime;

namespace SpreadsheetEditor.Core.Parser;


public static class ReferenceScanner
{
    public static IEnumerable<CellAddress> Find(string text)
    {
        var lexer = new FormulaLexer(new AntlrInputStream(text));
        lexer.RemoveErrorListeners();

        foreach (var token in lexer.GetAllTokens())
            if (token.Type == FormulaLexer.CELL && CellAddress.TryParse(token.Text, out var address))
                yield return address;
    }
}
