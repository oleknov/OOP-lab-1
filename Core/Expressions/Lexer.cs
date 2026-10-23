namespace SpreadsheetEditor.Core.Expressions;

internal enum TokenKind { Number, Identifier, Operator, LeftParen, RightParen, End }

internal readonly record struct Token(TokenKind Kind, string Text, int Position);

internal static class Lexer
{
    public static List<Token> Tokenize(string text)
    {
        var tokens = new List<Token>();
        int i = 0;
        while (i < text.Length)
        {
            char c = text[i];
            int start = i;
            if (char.IsWhiteSpace(c)) { i++; continue; }

            if (char.IsAsciiDigit(c))
            {
                while (i < text.Length && char.IsAsciiDigit(text[i])) i++;
                tokens.Add(new(TokenKind.Number, text[start..i], start));
            }
            else if (char.IsAsciiLetter(c))
            {
                while (i < text.Length && char.IsAsciiLetterOrDigit(text[i])) i++;
                tokens.Add(new(TokenKind.Identifier, text[start..i], start));
            }
            else
            {
                var kind = c switch
                {
                    '(' => TokenKind.LeftParen,
                    ')' => TokenKind.RightParen,
                    _ when Operations.IsOperatorSymbol(c) => TokenKind.Operator,
                    _ => throw new ExpressionException(i, $"недопустимий символ '{c}'")
                };
                tokens.Add(new(kind, c.ToString(), i++));
            }
        }
        tokens.Add(new(TokenKind.End, "", text.Length));
        return tokens;
    }
}
