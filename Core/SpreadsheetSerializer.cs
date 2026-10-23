using System.Text.Encodings.Web;
using System.Text.Json;

namespace SpreadsheetEditor.Core;


public static class SpreadsheetSerializer
{
    private sealed record TableDto(int Rows, int Columns, Dictionary<string, string> Cells);

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static string Serialize(Spreadsheet table) =>
        JsonSerializer.Serialize(
            new TableDto(table.RowCount, table.ColumnCount,
                table.GetContents().ToDictionary(p => p.Key.ToString(), p => p.Value)),
            Options);

    public static Spreadsheet Deserialize(string json)
    {
        try
        {
            var dto = JsonSerializer.Deserialize<TableDto>(json, Options)
                      ?? throw new InvalidDataException("Файл порожній.");
            var table = new Spreadsheet(dto.Rows, dto.Columns);
            table.Load(dto.Cells.Select(p => CellAddress.TryParse(p.Key, out var address)
                ? new KeyValuePair<CellAddress, string>(address, p.Value)
                : throw new InvalidDataException($"Невірна адреса клітинки '{p.Key}'.")));
            return table;
        }
        catch (Exception e) when (e is JsonException or ArgumentOutOfRangeException)
        {
            throw new InvalidDataException("Файл має невірний формат.", e);
        }
    }
}
