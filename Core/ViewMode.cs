namespace SpreadsheetEditor.Core;


public enum ViewMode { Expression, Value }

public static class ViewModeExtensions
{
    public static string Title(this ViewMode mode) =>
        mode == ViewMode.Expression ? "ВИРАЗ" : "ЗНАЧЕННЯ";
}
