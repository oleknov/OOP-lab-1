using SpreadsheetEditor.Core;
using SpreadsheetEditor.Views;

namespace SpreadsheetEditor.Controllers;


public sealed class EditingController
{
    private readonly SpreadsheetGrid grid;
    private readonly FormulaBar formulaBar;
    private readonly ValuePanel valuePanel;

    public EditingController(SpreadsheetGrid grid, FormulaBar formulaBar, ValuePanel valuePanel)
    {
        this.grid = grid;
        this.formulaBar = formulaBar;
        this.valuePanel = valuePanel;

        grid.CellEditingStarted += OnCellEditingStarted;
        grid.CellTextEdited += (_, expression) =>
        {
            formulaBar.Mirror(expression);
            HighlightReferences(expression);
        };
        grid.CellEditingFinished += OnCellEditingFinished;

        formulaBar.EditingStarted += (_, expression) => HighlightReferences(expression);
        formulaBar.TextEdited += (_, expression) => HighlightReferences(expression);
        formulaBar.EditingFinished += OnCellEditingFinished;
    }

    public Spreadsheet Table { get; private set; } = new(1, 1);
    public ViewMode Mode { get; private set; } = ViewMode.Value;

    public void ReplaceTable(Spreadsheet newTable)
    {
        Table = newTable;
        grid.BuildGrid(Table.RowCount, Table.ColumnCount);
        formulaBar.Reset();
        valuePanel.Reset();
        RefreshGrid();
    }

    public void Resize(int rows, int columns)
    {
        Table.Resize(rows, columns);
        ReplaceTable(Table);
    }

    public void ToggleMode()
    {
        Mode = Mode == ViewMode.Value ? ViewMode.Expression : ViewMode.Value;
        RefreshGrid();
    }

    private void OnCellEditingStarted(CellAddress address)
    {
        var expression = Table.GetCell(address.Row, address.Column).Expression;
        grid.SetEditorText(address, expression); 
        formulaBar.Show(address, expression);
        HighlightReferences(expression);
        UpdateSelectedCellPreview(address);
    }


    private void OnCellEditingFinished(CellAddress address, string text)
    {
        grid.ClearReferences();
        if (!Table.Contains(address)) return;

        if (text != Table.GetCell(address.Row, address.Column).Expression)
            Table.SetCellExpression(address.Row, address.Column, text);

        RefreshGrid();
        if (formulaBar.Address == address) formulaBar.Show(address, text);
        UpdateSelectedCellPreview(address);
    }

    private void HighlightReferences(string expression) =>
        grid.HighlightReferences(Table.GetReferencedCells(expression));

    private void UpdateSelectedCellPreview(CellAddress address)
    {
        var cell = Table.GetCell(address.Row, address.Column);
        if (cell.Error is null) valuePanel.ShowValue(address.ToString(), cell.FullValue);
        else valuePanel.ShowError(address.ToString(), cell.Error.Details);
    }

    private void RefreshGrid()
    {
        for (int r = 0; r < Table.RowCount; r++)
        {
            for (int c = 0; c < Table.ColumnCount; c++)
            {
                var cell = Table.GetCell(r, c);
                grid.Display(new CellAddress(r, c), cell.GetDisplayText(Mode), cell.Error is not null);
            }
        }
    }
}
