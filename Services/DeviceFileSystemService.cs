using System.Text;
using CommunityToolkit.Maui.Storage;

namespace SpreadsheetEditor.Services;

public sealed class DeviceFileSystemService : IFileSystemService
{
    public async Task<string?> PickFileAsync()
    {
        var result = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Оберіть таблицю" });
        return result?.FullPath;
    }

    public async Task<string?> WriteAsync(string? path, string suggestedName, string data)
    {
        if (path is not null)
        {
            await File.WriteAllTextAsync(path, data);
            return path;
        }

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(data));
        var result = await FileSaver.Default.SaveAsync(suggestedName, stream);
        return result.IsSuccessful ? result.FilePath : null;
    }

    public Task<string> ReadAsync(string path) => File.ReadAllTextAsync(path);
}
