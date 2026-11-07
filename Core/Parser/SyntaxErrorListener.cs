using Antlr4.Runtime;
using SpreadsheetEditor.Core.Tree;

namespace SpreadsheetEditor.Core.Parser;

internal sealed class SyntaxErrorListener(string text) : IAntlrErrorListener<int>, IAntlrErrorListener<IToken>
{

    public void SyntaxError(IRecognizer recognizer, int offendingSymbol,
        int line, int charPositionInLine, string msg, RecognitionException e)
    {
        int position = Math.Min(charPositionInLine, Math.Max(text.Length - 1, 0));
        var symbol = text.Length > 0 ? text[position].ToString() : "";
        throw new ExpressionException(position, $"недопустимий символ '{symbol}'");
    }

    public void SyntaxError(IRecognizer recognizer, IToken offendingSymbol,
        int line, int charPositionInLine, string msg, RecognitionException e)
    {
        var expected = ((Antlr4.Runtime.Parser)recognizer).GetExpectedTokens();
        int position = offendingSymbol.Type == TokenConstants.Eof ? text.Length : offendingSymbol.StartIndex;
        throw new ExpressionException(position, Describe(offendingSymbol, expected));
    }

    private static string Describe(IToken token, Antlr4.Runtime.Misc.IntervalSet expected)
    {
        bool atEnd = token.Type == TokenConstants.Eof;
        string found = atEnd ? "кінець виразу" : $"'{token.Text}'";

        if (token.Type == FormulaLexer.NAME)
            return $"невідомий ідентифікатор '{token.Text}' (адреса клітинки має вигляд A1, функції: inc, dec)";
        if (expected.Contains(FormulaLexer.NUMBER))
            return $"очікувалось число, клітинка або '(', а знайдено {found}";
        if (expected.Contains(FormulaLexer.LPAREN))
            return "після назви функції очікувалась '('";
        if (atEnd)
            return "очікувалась закриваюча дужка ')'";
        if (token.Type == FormulaLexer.RPAREN)
            return "зайва закриваюча дужка";
        return $"очікувалась операція, а знайдено {found}";
    }
}