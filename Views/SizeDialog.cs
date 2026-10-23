using Microsoft.Maui.Controls.Shapes;
using SpreadsheetEditor.Core;

namespace SpreadsheetEditor.Views;


public sealed class SizeDialog : Grid
{
    private readonly Label titleLabel = new() { FontSize = 20, FontAttributes = FontAttributes.Bold };
    private readonly Entry rowsEntry = CreateEntry();
    private readonly Entry columnsEntry = CreateEntry();
    private readonly Label errorLabel = new() { TextColor = Colors.Red, IsVisible = false };
    private TaskCompletionSource<(int Rows, int Columns)?>? completion;

    public SizeDialog()
    {
        IsVisible = false;
        BackgroundColor = new Color(0f, 0f, 0f, 0.45f); 
        rowsEntry.Completed += (_, _) => columnsEntry.Focus();
        columnsEntry.Completed += (_, _) => Accept();

        var ok = new Button { Text = "OK" };
        ok.Clicked += (_, _) => Accept();
        var cancel = new Button { Text = "Скасувати" };
        cancel.Clicked += (_, _) => Close(null);

        var card = new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(8) },
            Stroke = Color.FromArgb("#808080"),
            StrokeThickness = 1,
            Padding = 20,
            WidthRequest = 340,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Content = new VerticalStackLayout
            {
                Spacing = 10,
                Children =
                {
                    titleLabel,
                    new Label { Text = $"Кількість рядків (1–{Spreadsheet.MaxSize}):" },
                    rowsEntry,
                    new Label { Text = $"Кількість стовпців (1–{Spreadsheet.MaxSize}):" },
                    columnsEntry,
                    errorLabel,
                    new HorizontalStackLayout { Spacing = 8, Children = { ok, cancel } }
                }
            }
        };
        card.SetAppThemeColor(BackgroundColorProperty, Colors.White, Color.FromArgb("#2B2B2B"));
        Children.Add(card);
    }


    public Task<(int Rows, int Columns)?> ShowAsync(string title, int rows, int columns)
    {
        completion?.TrySetResult(null);
        completion = new TaskCompletionSource<(int Rows, int Columns)?>();

        titleLabel.Text = title;
        rowsEntry.Text = rows.ToString();
        columnsEntry.Text = columns.ToString();
        errorLabel.IsVisible = false;
        IsVisible = true;
        Dispatcher.Dispatch(() => rowsEntry.Focus());
        return completion.Task;
    }

    private void Accept()
    {
        if (int.TryParse(rowsEntry.Text, out var rows) && int.TryParse(columnsEntry.Text, out var columns) &&
            Spreadsheet.IsValidSize(rows) && Spreadsheet.IsValidSize(columns))
        {
            Close((rows, columns));
            return;
        }

        errorLabel.Text = $"Введіть цілі числа від 1 до {Spreadsheet.MaxSize}.";
        errorLabel.IsVisible = true;
    }

    private void Close((int Rows, int Columns)? result)
    {
        IsVisible = false;
        completion?.TrySetResult(result);
    }

    private static Entry CreateEntry() => new() { Keyboard = Keyboard.Numeric, MaxLength = 3 };
}
