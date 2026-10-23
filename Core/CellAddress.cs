using System.Globalization;

namespace SpreadsheetEditor.Core;


public readonly record struct CellAddress(int Row, int Column)
{
    private const int MaxLetters = 3;

    public static bool TryParse(string text, out CellAddress address)
    {
        address = default;
        int letters = 0, column = 0;
        while (letters < text.Length && char.IsAsciiLetter(text[letters]))
            column = column * 26 + (char.ToUpperInvariant(text[letters++]) - 'A' + 1);

        if (letters is 0 or > MaxLetters ||
            !int.TryParse(text.AsSpan(letters), NumberStyles.None, CultureInfo.InvariantCulture, out int row) ||
            row < 1)
            return false;

        address = new CellAddress(row - 1, column - 1);
        return true;
    }

    public static string ColumnName(int column)
    {
        var name = "";
        for (int n = column + 1; n > 0; n = (n - 1) / 26)
            name = (char)('A' + (n - 1) % 26) + name;
        return name;
    }

    public override string ToString() => $"{ColumnName(Column)}{Row + 1}";
}
