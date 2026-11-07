namespace SpreadsheetEditor.Views;


public sealed class ValuePanel : Grid
{
    private const string Placeholder = "Виберіть клітинку, щоб побачити її повне значення.";

    private readonly Label caption = new() { Text = "Значення", FontAttributes = FontAttributes.Bold };
    private readonly Editor content = new()
    {
        IsReadOnly = true,
        HeightRequest = 64,
        AutoSize = EditorAutoSizeOption.Disabled,
        Text = Placeholder
    };

    public ValuePanel()
    {
        RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        RowSpacing = 2;
        this.Add(caption, 0, 0);
        this.Add(content, 0, 1);
    }

    public void ShowValue(string address, string value) =>
        Set($"Значення {address}", string.IsNullOrEmpty(value) ? "(порожня клітинка)" : value, isError: false);

    public void ShowError(string address, string details) => Set($"Помилка в {address}", details, isError: true);

    public void Reset() => Set("Значення", Placeholder, isError: false);

    private void Set(string title, string text, bool isError)
    {
        caption.Text = title;
        content.Text = text;
        if (isError) content.TextColor = Colors.Red;
        else content.ClearValue(Editor.TextColorProperty);
    }
}
