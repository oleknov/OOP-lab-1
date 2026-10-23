namespace SpreadsheetEditor.Services;


public interface IFileSystemService
{
    Task<string?> PickFileAsync();
    Task<string?> WriteAsync(string? path, string suggestedName, string data);

    Task<string> ReadAsync(string path);
}
