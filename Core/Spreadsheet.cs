using System.Numerics;
using SpreadsheetEditor.Core.Expressions;
using IValueProvider = SpreadsheetEditor.Core.Expressions.IValueProvider;

namespace SpreadsheetEditor.Core;


public sealed class Spreadsheet : IValueProvider
{
    public const int MaxSize = 30;

    private Cell[,] cells;

    public Spreadsheet(int rows, int columns) => cells = CreateCells(rows, columns);

    public string Name { get; set; } = "Нова таблиця";
    public string? FilePath { get; set; }
    public string? DriveFileId { get; set; }
    public bool IsModified { get; private set; }

    public int RowCount => cells.GetLength(0);
    public int ColumnCount => cells.GetLength(1);

    public static bool IsValidSize(int size) => size is >= 1 and <= MaxSize;

    public bool Contains(CellAddress address) => address.Row < RowCount && address.Column < ColumnCount;

    public Cell GetCell(int row, int column) => cells[row, column];


    public IEnumerable<CellAddress> GetReferencedCells(string expression) =>
        ReferenceScanner.Find(expression).Where(Contains).Distinct();

    public bool SetCellExpression(int row, int column, string text)
    {
        cells[row, column].Define(text);
        IsModified = true;
        Recalculate();
        return cells[row, column].Error is null;
    }

    public bool WillLoseData(int rows, int columns)
    {
        for (int r = 0; r < RowCount; r++)
            for (int c = 0; c < ColumnCount; c++)
                if ((r >= rows || c >= columns) && !cells[r, c].IsEmpty) return true;
        return false;
    }

    public void Resize(int rows, int columns)
    {
        var resized = CreateCells(rows, columns);
        for (int r = 0; r < Math.Min(rows, RowCount); r++)
            for (int c = 0; c < Math.Min(columns, ColumnCount); c++)
                resized[r, c] = cells[r, c];
        cells = resized;
        IsModified = true;
        Recalculate();
    }


    public IEnumerable<KeyValuePair<CellAddress, string>> GetContents()
    {
        for (int r = 0; r < RowCount; r++)
            for (int c = 0; c < ColumnCount; c++)
                if (!cells[r, c].IsEmpty)
                    yield return new(new CellAddress(r, c), cells[r, c].Expression);
    }

    public void Load(IEnumerable<KeyValuePair<CellAddress, string>> contents)
    {
        foreach (var (address, text) in contents)
        {
            if (!Contains(address)) throw new InvalidDataException($"Клітинка {address} поза межами таблиці.");
            cells[address.Row, address.Column].Define(text);
        }
        Recalculate();
        IsModified = false;
    }

    public void MarkSaved() => IsModified = false;

    BigInteger IValueProvider.ValueOf(CellAddress address, int position)
    {
        if (!Contains(address))
            throw new ExpressionException(position, $"клітинки {address} немає в таблиці");

        var cell = cells[address.Row, address.Column];
        if (cell.IsCalculating)
            throw new ExpressionException(position, $"циклічне посилання на клітинку {address}");

        cell.Calculate(this);
        return cell.Error is null
            ? cell.Value
            : throw new ExpressionException(position, $"клітинка {address} містить помилку");
    }

    private void Recalculate()
    {
        foreach (var cell in cells) cell.Reset();
        foreach (var cell in cells) cell.Calculate(this);
    }

    private static Cell[,] CreateCells(int rows, int columns)
    {
        if (!IsValidSize(rows) || !IsValidSize(columns))
            throw new ArgumentOutOfRangeException(nameof(rows), $"Розмір таблиці: від 1 до {MaxSize}.");

        var result = new Cell[rows, columns];
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < columns; c++)
                result[r, c] = new Cell();
        return result;
    }
}
