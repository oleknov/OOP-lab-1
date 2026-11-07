namespace SpreadsheetEditor.Services;

public sealed class DialogService(Page page)
{
    private const string Ok = "OK", Yes = "Так", No = "Ні", Cancel = "Скасувати";

    public Task AlertAsync(string title, string message) => page.DisplayAlert(title, message, Ok);

    public Task<bool> ConfirmAsync(string title, string message) => ConfirmAsync(title, message, Yes, No);

    public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel) =>
        page.DisplayAlert(title, message, accept, cancel);

    public async Task<string?> ChooseAsync(string title, IEnumerable<string> options)
    {
        var choice = await page.DisplayActionSheet(title, Cancel, null, options.ToArray());
        return choice is null or Cancel ? null : choice;
    }

    public Task<string?> PromptAsync(string title, string message, string initialValue) =>
        page.DisplayPromptAsync(title, message, Ok, Cancel, initialValue: initialValue);

    public async Task RunSafelyAsync(Func<Task> action)
    {
        try { await action(); }
        catch (Exception ex) { await AlertAsync("Помилка", ex.Message); }
    }
}
