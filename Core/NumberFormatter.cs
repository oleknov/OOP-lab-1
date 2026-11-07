using System.Globalization;

namespace SpreadsheetEditor.Core;


public static class NumberFormatter
{
    public const int MaxPlainLength = 9;
    private const int SignificantDigits = 4; 

    public static string Compact(string full)
    {
        if (full.Length <= MaxPlainLength) return full;

        bool negative = full[0] == '-';
        var digits = negative ? full[1..] : full;
        int exponent = digits.Length - 1;

        int head = int.Parse(digits[..SignificantDigits], CultureInfo.InvariantCulture);
        if (digits[SignificantDigits] >= '5') head++;         
        if (head >= 10_000) { head /= 10; exponent++; }        

        var mantissa = head.ToString(CultureInfo.InvariantCulture);
        return $"{(negative ? "-" : "")}{mantissa[0]}.{mantissa[1..]}E+{exponent:D2}";
    }
}
