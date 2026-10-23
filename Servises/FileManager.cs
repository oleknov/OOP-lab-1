using SpreadsheetEditor.Core;

namespace SpreadsheetEditor.Services;


public sealed class FileManager(IFileSystemService system, GoogleDriveService drive, RecentFileList recent)
{
    private const string Extension = ".sprd";

    public IReadOnlyList<string> RecentFiles => recent.GetAll();

    public Task<string?> PickFileAsync() => system.PickFileAsync();

    public async Task<Spreadsheet> OpenAsync(string path)
    {
        var table = SpreadsheetSerializer.Deserialize(await system.ReadAsync(path));
        table.Name = Path.GetFileNameWithoutExtension(path);
        table.FilePath = path;
        recent.Add(path);
        return table;
    }

    public async Task<bool> SaveAsync(Spreadsheet table, bool saveAs)
    {
        var data = SpreadsheetSerializer.Serialize(table);
        var path = await system.WriteAsync(saveAs ? null : table.FilePath, table.Name + Extension, data);
        if (path is null) return false;

        table.FilePath = path;
        table.Name = Path.GetFileNameWithoutExtension(path);
        table.MarkSaved();
        recent.Add(path);
        return true;
    }

    public Task<IReadOnlyList<DriveEntry>> ListDriveFilesAsync() => drive.ListAsync();

    public async Task<Spreadsheet> OpenFromDriveAsync(DriveEntry entry)
    {
        var table = SpreadsheetSerializer.Deserialize(await drive.DownloadAsync(entry.Id));
        table.Name = Path.GetFileNameWithoutExtension(entry.Name);
        table.DriveFileId = entry.Id;
        return table;
    }

    public async Task SaveToDriveAsync(Spreadsheet table)
    {
        var data = SpreadsheetSerializer.Serialize(table);
        table.DriveFileId = await drive.UploadAsync(table.Name + Extension, data, table.DriveFileId);
        table.MarkSaved();
    }
}
