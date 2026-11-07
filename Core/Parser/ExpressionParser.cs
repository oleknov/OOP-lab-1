using Antlr4.Runtime;
using SpreadsheetEditor.Core.Tree;

namespace SpreadsheetEditor.Core.Parser;

internal static class ExpressionParser
{
    public static Node Parse(string expression)
    {
        var errors = new SyntaxErrorListener(expression);

        var lexer = new FormulaLexer(new AntlrInputStream(expression));
        lexer.RemoveErrorListeners();
        lexer.AddErrorListener(errors);

        var parser = new FormulaParser(new CommonTokenStream(lexer));
        parser.RemoveErrorListeners();
        parser.AddErrorListener(errors);

        return new TreeBuilder().Visit(parser.formula());
    }
}
