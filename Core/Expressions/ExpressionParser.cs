using System.Numerics;

namespace SpreadsheetEditor.Core.Expressions;


internal sealed class ExpressionParser
{
    private const string MissingParen = "очікувалась закриваюча дужка ')'";

    private readonly List<Token> tokens;
    private int index;

    private ExpressionParser(List<Token> tokens) => this.tokens = tokens;

    public static Node Parse(string expression)
    {
        var parser = new ExpressionParser(Lexer.Tokenize(expression));
        var tree = parser.ParseExpression(0);
        var rest = parser.Current;
        if (rest.Kind != TokenKind.End)
            throw new ExpressionException(rest.Position,
                rest.Kind == TokenKind.RightParen ? "зайва закриваюча дужка" : "очікувалась операція");
        return tree;
    }

    private Token Current => tokens[index];

    private Token Advance()
    {
        var token = tokens[index];
        if (token.Kind != TokenKind.End) index++;
        return token;
    }

    private void Expect(TokenKind kind, string message)
    {
        if (Current.Kind != kind) throw new ExpressionException(Current.Position, message);
        Advance();
    }

    private Node ParseExpression(int minPriority)
    {
        var left = ParseOperand();
        while (Current.Kind == TokenKind.Operator &&
               Operations.TryGetBinary(Current.Text, out var operation) &&
               operation.Priority >= minPriority)
        {
            var token = Advance();
            var right = ParseExpression(operation.RightAssociative ? operation.Priority : operation.Priority + 1);
            left = new BinaryNode(operation, left, right, token.Position);
        }
        return left;
    }

    private Node ParseOperand()
    {
        var token = Advance();
        switch (token.Kind)
        {
            case TokenKind.Number:
                return new NumberNode(BigInteger.Parse(token.Text));

            case TokenKind.Identifier:
                return Current.Kind == TokenKind.LeftParen ? ParseFunction(token) : ParseReference(token);

            case TokenKind.LeftParen:
                var inner = ParseExpression(0);
                Expect(TokenKind.RightParen, MissingParen);
                return inner;

            case TokenKind.Operator when Operations.TryGetPrefix(token.Text, out var prefix):
                return new UnaryNode(prefix, ParseExpression(Operations.PowerPriority), token.Position);

            default:
                throw new ExpressionException(token.Position,
                    token.Kind == TokenKind.End ? "несподіваний кінець виразу" : $"несподіваний символ '{token.Text}'");
        }
    }

    private Node ParseFunction(Token name)
    {
        if (!Operations.TryGetFunction(name.Text, out var function))
            throw new ExpressionException(name.Position, $"невідома функція '{name.Text}'");
        Advance();
        var argument = ParseExpression(0);
        Expect(TokenKind.RightParen, MissingParen);
        return new UnaryNode(function, argument, name.Position);
    }

    private static Node ParseReference(Token name) =>
        CellAddress.TryParse(name.Text, out var address)
            ? new ReferenceNode(address, name.Position)
            : throw new ExpressionException(name.Position, $"невідомий ідентифікатор '{name.Text}'");
}
