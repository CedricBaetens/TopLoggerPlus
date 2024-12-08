namespace TopLoggerPlus.App.Utils;

public interface IDialogService
{
    void Init(Shell mainShell);
    Task DisplayAlert(string title, string message = null);
}

public class DialogService : IDialogService
{
    private Shell _shell;

    public void Init(Shell shell)
    {
        _shell = shell;
    }
    
    public Task DisplayAlert(string title, string message)
    {
        return _shell?.DisplayAlert(title, message, "Ok") ?? Task.CompletedTask;
    }
}