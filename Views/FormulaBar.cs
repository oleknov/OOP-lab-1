using SpreadsheetEditor.Core;

namespace SpreadsheetEditor.Views;

public sealed class FormulaBar : Grid
{
    private readonly Label addressLabel = new()
    {
        Text = "—", FontAttributes = FontAttributes.Bold, VerticalTextAlignment = TextAlignment.Center
    };
    private readonly Entry editor = new() { Placeholder = "Виберіть клітинку та введіть вираз…", IsEnabled = false };

    public FormulaBar()
    {
        ColumnDefinitions.Add(new ColumnDefinition(56));
        ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        ColumnSpacing = 8;
        this.Add(addressLabel, 0);
        this.Add(editor, 1);

        editor.Focused += (_, _) => { if (Address is { } a) EditingStarted?.Invoke(a, editor.Text ?? ""); };
        editor.TextChanged += (_, e) =>
        {
            if (editor.IsFocused && Address is { } a) TextEdited?.Invoke(a, e.NewTextValue ?? "");
        };
        editor.Unfocused += (_, _) => Finish();
        editor.Completed += (_, _) => Finish();
    }

    public CellAddress? Address { get; private set; }

    public event Action<CellAddress, string>? EditingStarted;
    public event Action<CellAddress, string>? TextEdited;
    public event Action<CellAddress, string>? EditingFinished;

    public void Show(CellAddress address, string expression)
    {
        Address = address;
        addressLabel.Text = address.ToString();
        editor.IsEnabled = true;
        editor.Text = expression;
    }

    public void Mirror(string text)
    {
        if (!editor.IsFocused) editor.Text = text;
    }

    public void Reset()
    {
        Address = null;
        addressLabel.Text = "—";
        editor.Text = "";
        editor.IsEnabled = false;
    }

    private void Finish()
    {
        if (Address is { } a) EditingFinished?.Invoke(a, editor.Text ?? "");
    }
}
