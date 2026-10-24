using SpreadsheetEditor.Core;
using SpreadsheetEditor.Services;
using SpreadsheetEditor.Views;

namespace SpreadsheetEditor;
public partial class MainPage : ContentPage
{
    private const int DefaultSize = 10;
    private const string Cancel = "Скасувати";

    private readonly FileManager files = new(new DeviceFileSystemService(), new GoogleDriveService(), new RecentFileList());
    private readonly HelpProvider help = new();
    private readonly List<CellView> referenced = [];

    private Spreadsheet table = new(DefaultSize, DefaultSize);
    private CellView[,] views = new CellView[0, 0];
    private CellView? editingView;
    private ViewMode mode = ViewMode.Value;

    public MainPage()
    {
        InitializeComponent();
        ModeButton.Text = ModeCaption();
        BuildGrid();
    }

    private async void OnNewClicked(object? sender, EventArgs e)
    {
        var size = await AskDimensionsAsync("Нова таблиця");
        if (size is null || !await ConfirmDiscardAsync()) return;

        var (rows, columns) = size.Value;
        ReplaceTable(new Spreadsheet(rows, columns));
    }

    private async void OnResizeClicked(object? sender, EventArgs e)
    {
        var size = await AskDimensionsAsync("Розмір таблиці");
        if (size is null) return;
        var (rows, columns) = size.Value;

        if (table.WillLoseData(rows, columns) &&
            !await DisplayAlert("Втрата даних", "Зменшення таблиці видалить частину даних. Продовжити?", "Так", "Ні"))
            return;

        table.Resize(rows, columns);
        BuildGrid();
    }

    private async void OnOpenClicked(object? sender, EventArgs e)
    {
        if (!await ConfirmDiscardAsync()) return;
        await RunSafelyAsync(async () =>
        {
            var path = await files.PickFileAsync();
            if (path is not null) ReplaceTable(await files.OpenAsync(path));
        });
    }

    private async void OnOpenRecentClicked(object? sender, EventArgs e)
    {
        var recent = files.RecentFiles;
        if (recent.Count == 0)
        {
            await DisplayAlert("Нещодавні таблиці", "Список порожній.", "OK");
            return;
        }

        var path = await DisplayActionSheet("Нещодавні таблиці", Cancel, null, recent.ToArray());
        if (path is null or Cancel || !await ConfirmDiscardAsync()) return;
        await RunSafelyAsync(async () => ReplaceTable(await files.OpenAsync(path)));
    }

    private async void OnSaveClicked(object? sender, EventArgs e) => await SaveLocallyAsync(saveAs: false);

    private async void OnSaveAsClicked(object? sender, EventArgs e) => await SaveLocallyAsync(saveAs: true);

    private async void OnOpenDriveClicked(object? sender, EventArgs e)
    {
        if (!await ConfirmDiscardAsync()) return;
        await RunSafelyAsync(async () =>
        {
            var list = await files.ListDriveFilesAsync();
            if (list.Count == 0)
            {
                await DisplayAlert("Google Drive", "Збережених таблиць не знайдено.", "OK");
                return;
            }

            var name = await DisplayActionSheet("Google Drive", Cancel, null, list.Select(f => f.Name).ToArray());
            var entry = list.FirstOrDefault(f => f.Name == name);
            if (entry is not null) ReplaceTable(await files.OpenFromDriveAsync(entry));
        });
    }

    private async void OnSaveDriveClicked(object? sender, EventArgs e)
    {
        await RunSafelyAsync(async () =>
        {
            if (table.DriveFileId is null)
            {
                var name = await DisplayPromptAsync("Google Drive", "Назва файлу:", "OK", Cancel, initialValue: table.Name);
                if (string.IsNullOrWhiteSpace(name)) return;
                table.Name = name.Trim();
            }

            await files.SaveToDriveAsync(table);
            StatusLabel.Text = $"Збережено на Google Drive: {table.Name}";
        });
    }

    private void OnSwitchModeClicked(object? sender, EventArgs e)
    {
        mode = mode == ViewMode.Value ? ViewMode.Expression : ViewMode.Value;
        ModeButton.Text = ModeCaption();
        RefreshGrid();
        if (editingView is not null)
        {
            for (int r = 0; r < views.GetLength(0); r++)
                for (int c = 0; c < views.GetLength(1); c++)
                    if (ReferenceEquals(views[r, c], editingView)) UpdateSelectedCellPreview(r, c, editingView.Text);
        }
    }

    private async void OnHelpClicked(object? sender, EventArgs e) =>
        await DisplayAlert("Довідка", help.GetHelpText(), "OK");

    private void BuildGrid()
    {
        TableGrid.Children.Clear();
        TableGrid.RowDefinitions.Clear();
        TableGrid.ColumnDefinitions.Clear();

        for (int r = 0; r <= table.RowCount; r++)
            TableGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        for (int c = 0; c <= table.ColumnCount; c++)
            TableGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(c == 0 ? 44 : 110)));

        for (int c = 0; c < table.ColumnCount; c++)
            TableGrid.Add(CreateHeader(CellAddress.ColumnName(c)), c + 1, 0);
        for (int r = 0; r < table.RowCount; r++)
            TableGrid.Add(CreateHeader((r + 1).ToString()), 0, r + 1);

        views = new CellView[table.RowCount, table.ColumnCount];
        referenced.Clear();
        editingView = null;
        for (int r = 0; r < table.RowCount; r++)
        {
            for (int c = 0; c < table.ColumnCount; c++)
            {
                int row = r, column = c;
                var view = new CellView();
                view.EditingStarted += () => OnCellEditingStarted(row, column);
                view.EditingFinished += () => OnCellEditingFinished(view, row, column);
                view.TextEdited += expression =>
                {
                    HighlightReferences(expression);
                    UpdateSelectedCellPreview(row, column, expression);
                };
                views[r, c] = view;
                TableGrid.Add(view, c + 1, r + 1);
            }
        }

        RefreshGrid();
        SelectedCellLabel.Text = "Клітинка: —";
        SelectedCellContentLabel.Text = "Виберіть клітинку, щоб побачити її повний вміст.";
        StatusLabel.Text = table.Name;
    }

    private void RefreshGrid()
    {
        for (int r = 0; r < table.RowCount; r++)
        {
            for (int c = 0; c < table.ColumnCount; c++)
            {
                var cell = table.GetCell(r, c);
                views[r, c].Display(cell.GetDisplayText(mode), cell.Error is not null);
            }
        }
    }

    private void OnCellEditingStarted(int row, int column)
    {
        ClearReferences(); 
        var expression = table.GetCell(row, column).Expression;
        editingView = views[row, column];
        editingView.Text = expression; 
        HighlightReferences(expression);
        UpdateSelectedCellPreview(row, column, expression);
        ShowStatus(row, column);
    }

    private void OnCellEditingFinished(CellView view, int row, int column)
    {
        if (!IsCurrent(view, row, column)) return;

        if (editingView == view)
        {
            ClearReferences();
            editingView = null;
        }

        if (view.Text != table.GetCell(row, column).Expression)
            table.SetCellExpression(row, column, view.Text);

        RefreshGrid();
        if (editingView is null)
        {
            UpdateSelectedCellPreview(row, column);
            ShowStatus(row, column);
        }
    }

    private void UpdateSelectedCellPreview(int row, int column, string? editingExpression = null)
    {
        if (row < 0 || column < 0 || row >= table.RowCount || column >= table.ColumnCount)
            return;

        var address = new CellAddress(row, column);
        var cell = table.GetCell(row, column);
        SelectedCellLabel.Text = $"Клітинка {address}";


        var content = editingExpression ?? cell.GetDisplayText(mode);
        if (string.IsNullOrEmpty(content)) content = "(порожня клітинка)";
        SelectedCellContentLabel.Text = content;
    }

    private bool IsCurrent(CellView view, int row, int column) =>
        row < views.GetLength(0) && column < views.GetLength(1) && ReferenceEquals(views[row, column], view);


    private void HighlightReferences(string expression)
    {
        ClearReferences();
        foreach (var address in table.GetReferencedCells(expression))
        {
            var view = views[address.Row, address.Column];
            view.IsHighlighted = true;
            referenced.Add(view);
        }
    }

    private void ClearReferences()
    {
        foreach (var view in referenced) view.IsHighlighted = false;
        referenced.Clear();
    }

    private void ShowStatus(int row, int column)
    {
        var address = new CellAddress(row, column);
        var error = table.GetCell(row, column).Error;
        StatusLabel.Text = error is null ? address.ToString() : $"{address} — помилка, {error.Details}";
    }

    private static Label CreateHeader(string text) => new()
    {
        Text = text,
        FontAttributes = FontAttributes.Bold,
        HorizontalTextAlignment = TextAlignment.Center,
        VerticalTextAlignment = TextAlignment.Center,
        BackgroundColor = Colors.LightGray,
        TextColor = Colors.Black
    };



    private void ReplaceTable(Spreadsheet newTable)
    {
        table = newTable;
        BuildGrid();
    }

    private string ModeCaption() => $"Режим: {mode.Title()}";

    private async Task<bool> ConfirmDiscardAsync() =>
        !table.IsModified ||
        await DisplayAlert("Незбережені зміни", "Є незбережені зміни. Продовжити без збереження?", "Так", "Ні");

    private Task<(int Rows, int Columns)?> AskDimensionsAsync(string title) =>
        ResizeDialog.ShowAsync(title, table.RowCount, table.ColumnCount);

    private async Task SaveLocallyAsync(bool saveAs) =>
        await RunSafelyAsync(async () =>
        {
            if (await files.SaveAsync(table, saveAs))
                StatusLabel.Text = $"Збережено: {table.FilePath}";
        });

    private async Task RunSafelyAsync(Func<Task> action)
    {
        try { await action(); }
        catch (Exception ex) { await DisplayAlert("Помилка", ex.Message, "OK"); }
    }
}
