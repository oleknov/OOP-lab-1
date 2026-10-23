using System.Text;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Download;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;
using DriveFile = Google.Apis.Drive.v3.Data.File;

namespace SpreadsheetEditor.Services;

public sealed record DriveEntry(string Id, string Name);

public sealed class GoogleDriveService
{
    private const string MimeType = "application/json";
    private DriveService? service;

    public async Task<IReadOnlyList<DriveEntry>> ListAsync()
    {
        var request = (await GetServiceAsync()).Files.List();
        request.Q = $"trashed = false and mimeType = '{MimeType}'";
        request.OrderBy = "modifiedTime desc";
        request.PageSize = 20;
        request.Fields = "files(id, name)";
        var response = await request.ExecuteAsync();
        return response.Files.Select(f => new DriveEntry(f.Id, f.Name)).ToList();
    }

    public async Task<string> DownloadAsync(string fileId)
    {
        using var stream = new MemoryStream();
        var progress = await (await GetServiceAsync()).Files.Get(fileId).DownloadAsync(stream);
        if (progress.Status == DownloadStatus.Failed) throw progress.Exception;
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    public async Task<string> UploadAsync(string name, string data, string? fileId)
    {
        var drive = await GetServiceAsync();
        using var content = new MemoryStream(Encoding.UTF8.GetBytes(data));

        if (fileId is null)
        {
            var create = drive.Files.Create(new DriveFile { Name = name, MimeType = MimeType }, content, MimeType);
            create.Fields = "id";
            EnsureSucceeded(await create.UploadAsync());
            return create.ResponseBody.Id;
        }

        EnsureSucceeded(await drive.Files.Update(new DriveFile(), fileId, content, MimeType).UploadAsync());
        return fileId;
    }

    private async Task<DriveService> GetServiceAsync()
    {
        if (service is not null) return service;

        Stream secretsStream;
        try
        {
            secretsStream = await FileSystem.OpenAppPackageFileAsync("credentials.json");
        }
        catch (FileNotFoundException ex)
        {
            throw new InvalidOperationException(
                "Не знайдено credentials.json. Додайте OAuth credentials до Resources/Raw/credentials.json і налаштуйте Google Drive.",
                ex);
        }

        await using var ownedSecretsStream = secretsStream;
        var secrets = GoogleClientSecrets.FromStream(ownedSecretsStream).Secrets;
        var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            secrets, [DriveService.Scope.DriveFile], "user", CancellationToken.None,
            new FileDataStore(Path.Combine(FileSystem.AppDataDirectory, "google-token"), true));

        return service = new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "SpreadsheetEditor"
        });
    }

    private static void EnsureSucceeded(IUploadProgress progress)
    {
        if (progress.Status == UploadStatus.Failed) throw progress.Exception;
    }
}
