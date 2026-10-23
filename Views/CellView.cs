namespace SpreadsheetEditor.Views;

/// <summary>Візуальна клітинка: поле вводу в рамці, яку можна підсвітити блакитним.</summary>
public sealed class CellView : Border
{
    private static readonly Color HighlightStroke = Color.FromArgb("#1E90FF");
    private static readonly Color HighlightFill = new(0.27f, 0.63f, 1f, 0.30f);

    private readonly Entry editor = new() { HorizontalTextAlignment = TextAlignment.Start };
    private bool isHighlighted;

    public CellView()
    {
        Stroke = Colors.Transparent;
        StrokeThickness = 2;
        Padding = 0;
        Content = editor;

        editor.Focused += (_, _) => EditingStarted?.Invoke();
        editor.Unfocused += (_, _) => EditingFinished?.Invoke();
        editor.TextChanged += (_, e) =>
        {
            if (editor.IsFocused) TextEdited?.Invoke(e.NewTextValue ?? "");
        };
    }

    public event Action? EditingStarted;
    public event Action? EditingFinished;
    public event Action<string>? TextEdited;

    public bool IsEditing => editor.IsFocused;

    public string Text
    {
        get => editor.Text ?? "";
        set { if (editor.Text != value) editor.Text = value; }
    }

    /// <summary>Блакитна обводка й заливка: клітинка, на яку посилається вираз, що редагується.</summary>
    public bool IsHighlighted
    {
        get => isHighlighted;
        set
        {
            if (isHighlighted == value) return;
            isHighlighted = value;

            Stroke = value ? HighlightStroke : Colors.Transparent;
            if (value) editor.BackgroundColor = HighlightFill;
            else editor.ClearValue(VisualElement.BackgroundColorProperty);
        }
    }

    /// <summary>Показує текст і стиль помилки; змінює лише те, що справді змінилось.</summary>
    public void Display(string text, bool isError)
    {
        if (IsEditing) return; // не заважаємо введенню

        Text = text;
        if (isError)
        {
            if (editor.TextColor != Colors.Red) editor.TextColor = Colors.Red;
        }
        else if (editor.IsSet(Entry.TextColorProperty))
        {
            editor.ClearValue(Entry.TextColorProperty);
        }
    }
}
