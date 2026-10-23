using System.Text.Json;

namespace SpreadsheetEditor.Services;


public sealed class RecentFileList(int maxCount = 5)
{
    private const string Key = "recent_files";

    public IReadOnlyList<string> GetAll() =>
        (JsonSerializer.Deserialize<List<string>>(Preferences.Get(Key, "[]")) ?? [])
        .Where(File.Exists)
        .ToList();

    public void Add(string path)
    {
        var updated = GetAll().Where(p => p != path).Prepend(path).Take(maxCount);
        Preferences.Set(Key, JsonSerializer.Serialize(updated));
    }
}
