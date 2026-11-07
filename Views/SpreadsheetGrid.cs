using SpreadsheetEditor.Core;

namespace SpreadsheetEditor.Views;

public sealed class SpreadsheetGrid : ScrollView
{
    private const double CellWidth = 110, CellHeight = 36, HeaderWidth = 44, HeaderHeight = 28;

    private readonly Grid tableGrid = new() { ColumnSpacing = 2, RowSpacing = 2 };
    private readonly List<CellView> referenced = [];
    private CellView[,] views = new CellView[0, 0];

    public SpreadsheetGrid()
    {
        Orientation = ScrollOrientation.Both;
        Content = tableGrid;
    }

    public event Action<CellAddress>? CellEditingStarted;
    public event Action<CellAddress, string>? CellTextEdited;
    public event Action<CellAddress, string>? CellEditingFinished;

    public void BuildGrid(int rows, int columns)
    {
        tableGrid.Children.Clear();
        tableGrid.RowDefinitions.Clear();
        tableGrid.ColumnDefinitions.Clear();
        referenced.Clear();

        tableGrid.RowDefinitions.Add(new RowDefinition(HeaderHeight));
        for (int r = 0; r < rows; r++) tableGrid.RowDefinitions.Add(new RowDefinition(CellHeight));
        tableGrid.ColumnDefinitions.Add(new ColumnDefinition(HeaderWidth));
        for (int c = 0; c < columns; c++) tableGrid.ColumnDefinitions.Add(new ColumnDefinition(CellWidth));

        for (int c = 0; c < columns; c++) tableGrid.Add(CreateHeader(CellAddress.ColumnName(c)), c + 1, 0);
        for (int r = 0; r < rows; r++) tableGrid.Add(CreateHeader((r + 1).ToString()), 0, r + 1);

        views = new CellView[rows, columns];
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                var address = new CellAddress(r, c);
                var view = new CellView(CellWidth, CellHeight);
                view.EditingStarted += () => CellEditingStarted?.Invoke(address);
                view.TextEdited += text => CellTextEdited?.Invoke(address, text);
                view.EditingFinished += () => CellEditingFinished?.Invoke(address, view.Text);
                views[r, c] = view;
                tableGrid.Add(view, c + 1, r + 1);
            }
        }
    }

    public void Display(CellAddress address, string text, bool isError) =>
        views[address.Row, address.Column].Display(text, isError);

    public void SetEditorText(CellAddress address, string text) => views[address.Row, address.Column].Text = text;

    public void HighlightReferences(IEnumerable<CellAddress> addresses)
    {
        ClearReferences();
        foreach (var address in addresses)
        {
            var view = views[address.Row, address.Column];
            view.IsHighlighted = true;
            referenced.Add(view);
        }
    }

    public void ClearReferences()
    {
        foreach (var view in referenced) view.IsHighlighted = false;
        referenced.Clear();
    }

    private static Label CreateHeader(string text) => new()
    {
        Text = text,
        HorizontalTextAlignment = TextAlignment.Center,
        VerticalTextAlignment = TextAlignment.Center,
        Opacity = 0.75
    };
}
