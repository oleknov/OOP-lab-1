using Microsoft.Maui.Controls.Shapes;

namespace SpreadsheetEditor.Views;

public sealed class CellView : Border
{
    private static readonly Color HighlightStroke = Color.FromArgb("#1E90FF");
    private static readonly Color HighlightFill = new(0.27f, 0.63f, 1f, 0.30f);
    private static readonly Color NormalStroke = Color.FromArgb("#40808080");

    private readonly Entry editor = new()
    {
        HorizontalTextAlignment = TextAlignment.Start,
        VerticalTextAlignment = TextAlignment.Center
    };
    private bool isHighlighted;

    public CellView(double width, double height)
    {
        WidthRequest = width;
        HeightRequest = height;
        Stroke = NormalStroke;
        StrokeShape = new RoundRectangle { CornerRadius = 3 };
        StrokeThickness = 2;
        Padding = 0;
        Content = editor;

        editor.Focused += (_, _) => EditingStarted?.Invoke();
        editor.Unfocused += (_, _) => EditingFinished?.Invoke();
        editor.Completed += (_, _) => EditingFinished?.Invoke(); 
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


    public bool IsHighlighted
    {
        get => isHighlighted;
        set
        {
            if (isHighlighted == value) return;
            isHighlighted = value;

            Stroke = value ? HighlightStroke : NormalStroke;
            if (value) editor.BackgroundColor = HighlightFill;
            else editor.ClearValue(VisualElement.BackgroundColorProperty);
        }
    }

   
    public void Display(string text, bool isError)
    {
        if (IsEditing) return; 

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
