using SpreadsheetEditor.Controllers;
using SpreadsheetEditor.Core;
using SpreadsheetEditor.Services;

namespace SpreadsheetEditor;

public partial class MainPage : ContentPage
{
    private const int DefaultSize = 10;

    private readonly DialogService dialogs;
    private readonly FileController files;
    private readonly EditingController editor;
    private readonly HelpProvider help = new();

    public MainPage()
    {
        InitializeComponent();
        dialogs = new DialogService(this);
        files = new FileController(
            new FileManager(new DeviceFileSystemService(), new GoogleDriveService(), new RecentFileList()), dialogs);
        editor = new EditingController(CellsGrid, FormulaBox, ValueBox);

        ModeButton.Text = ModeCaption();
        ReplaceTable(new Spreadsheet(DefaultSize, DefaultSize));
    }

    private Spreadsheet Table => editor.Table;

    private async void OnNewClicked(object? sender, EventArgs e)
    {
        var size = await AskDimensionsAsync("Нова таблиця");
        if (size is null || !await files.ConfirmDiscardAsync(Table)) return;

        var (rows, columns) = size.Value;
        ReplaceTable(new Spreadsheet(rows, columns));
    }

    private async void OnResizeClicked(object? sender, EventArgs e)
    {
        var size = await AskDimensionsAsync("Розмір таблиці");
        if (size is null) return;
        var (rows, columns) = size.Value;

        if (Table.WillLoseData(rows, columns) &&
            !await dialogs.ConfirmAsync("Втрата даних", "Зменшення таблиці видалить частину даних. Продовжити?"))
            return;

        editor.Resize(rows, columns);
    }

    private async void OnOpenClicked(object? sender, EventArgs e) => ReplaceTable(await files.OpenAsync(Table));

    private async void OnOpenRecentClicked(object? sender, EventArgs e) => ReplaceTable(await files.OpenRecentAsync(Table));

    private async void OnSaveClicked(object? sender, EventArgs e) => SetStatus(await files.SaveLocallyAsync(Table, saveAs: false));

    private async void OnSaveAsClicked(object? sender, EventArgs e) => SetStatus(await files.SaveLocallyAsync(Table, saveAs: true));

    private async void OnOpenDriveClicked(object? sender, EventArgs e) => ReplaceTable(await files.OpenFromDriveAsync(Table));

    private async void OnSaveDriveClicked(object? sender, EventArgs e) => SetStatus(await files.SaveToDriveAsync(Table));

    private void OnSwitchModeClicked(object? sender, EventArgs e)
    {
        editor.ToggleMode();
        ModeButton.Text = ModeCaption();
    }

    private async void OnHelpClicked(object? sender, EventArgs e) =>
        await DisplayAlert("Довідка", help.GetHelpText(), "OK");

    private void ReplaceTable(Spreadsheet? newTable)
    {
        if (newTable is null) return; 
        editor.ReplaceTable(newTable);
        StatusLabel.Text = newTable.Name;
    }

    private string ModeCaption() => $"Режим: {editor.Mode.Title()}";

    private Task<(int Rows, int Columns)?> AskDimensionsAsync(string title) =>
        ResizeDialog.ShowAsync(title, Table.RowCount, Table.ColumnCount);

    private async void OnExitClicked(object? sender, EventArgs e)
    {
        if (await files.ConfirmExitAsync(Table)) Application.Current?.Quit();
    }

    private void SetStatus(string? text)
    {
        if (text is not null) StatusLabel.Text = text;
    }
}
