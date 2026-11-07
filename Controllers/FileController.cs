using SpreadsheetEditor.Core;
using SpreadsheetEditor.Services;

namespace SpreadsheetEditor.Controllers;

    
public sealed class FileController(FileManager files, DialogService dialogs)
{
    public async Task<bool> ConfirmDiscardAsync(Spreadsheet table) =>
        !table.IsModified ||
        await dialogs.ConfirmAsync("Незбережені зміни", "Є незбережені зміни. Продовжити без збереження?");

    public Task<Spreadsheet?> OpenAsync(Spreadsheet current) => OpenWithAsync(current, async () =>
    {
        var path = await files.PickFileAsync();
        return path is null ? null : await files.OpenAsync(path);
    });

    public Task<Spreadsheet?> OpenRecentAsync(Spreadsheet current) => OpenWithAsync(current, async () =>
    {
        var recent = files.RecentFiles;
        if (recent.Count == 0)
        {
            await dialogs.AlertAsync("Нещодавні таблиці", "Список порожній.");
            return null;
        }
        var path = await dialogs.ChooseAsync("Нещодавні таблиці", recent);
        return path is null ? null : await files.OpenAsync(path);
    });

    public Task<Spreadsheet?> OpenFromDriveAsync(Spreadsheet current) => OpenWithAsync(current, async () =>
    {
        var list = await files.ListDriveFilesAsync();
        if (list.Count == 0)
        {
            await dialogs.AlertAsync("Google Drive", "Збережених таблиць не знайдено.");
            return null;
        }
        var name = await dialogs.ChooseAsync("Google Drive", list.Select(f => f.Name));
        var entry = list.FirstOrDefault(f => f.Name == name);
        return entry is null ? null : await files.OpenFromDriveAsync(entry);
    });

    public async Task<string?> SaveLocallyAsync(Spreadsheet table, bool saveAs)
    {
        string? status = null;
        await dialogs.RunSafelyAsync(async () =>
        {
            if (await files.SaveAsync(table, saveAs)) status = $"Збережено: {table.FilePath}";
        });
        return status;
    }

    public async Task<string?> SaveToDriveAsync(Spreadsheet table)
    {
        string? status = null;
        await dialogs.RunSafelyAsync(async () =>
        {
            if (table.DriveFileId is null)
            {
                var name = await dialogs.PromptAsync("Google Drive", "Назва файлу:", table.Name);
                if (string.IsNullOrWhiteSpace(name)) return;
                table.Name = name.Trim();
            }

            await files.SaveToDriveAsync(table);
            status = $"Збережено на Google Drive: {table.Name}";
        });
        return status;
    }

    public async Task<bool> ConfirmExitAsync(Spreadsheet table) =>
        !table.IsModified ||
        await dialogs.ConfirmAsync("Незбережені зміни",
            $"Таблицю «{table.Name}» не збережено. Якщо вийти зараз, зміни буде втрачено.",
            accept: "Все одно вийти", cancel: "Скасувати");

    private async Task<Spreadsheet?> OpenWithAsync(Spreadsheet current, Func<Task<Spreadsheet?>> open)
    {
        if (!await ConfirmDiscardAsync(current)) return null;
        Spreadsheet? result = null;
        await dialogs.RunSafelyAsync(async () => result = await open());
        return result;
    }
}
