using System.Text.RegularExpressions;

namespace SpreadsheetEditor.Core.Expressions;


public static class ReferenceScanner
{
    private static readonly Regex Pattern = new(@"(?<![A-Za-z0-9])[A-Za-z]{1,3}[0-9]+(?![A-Za-z0-9(])");

    public static IEnumerable<CellAddress> Find(string text)
    {
        foreach (Match match in Pattern.Matches(text))
            if (CellAddress.TryParse(match.Value, out var address))
                yield return address;
    }
}
