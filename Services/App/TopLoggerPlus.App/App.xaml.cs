using TopLoggerPlus.App.Utils;

namespace TopLoggerPlus.App;

public partial class App : Application
{
    private readonly AppShellViewModel _vm;
    private readonly IDialogService _dialogService;

    public App(AppShellViewModel vm, IDialogService dialogService)
    {
        _vm = vm;
        _dialogService = dialogService;
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState activationState)
    {
        var shell = new AppShell(_vm);
        _dialogService.Init(shell);
        return new Window(shell);
    }
}
