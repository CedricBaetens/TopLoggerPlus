using Refit;
using TopLoggerPlus.Contracts.Services.TopLogger;

namespace TopLoggerPlus.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddSingleton<OverviewViewModel>();
        builder.Services.AddSingleton<OverviewPage>();

        builder.Services.AddTransient<IRouteService, RouteService>();
        builder.Services.AddTransient<ITopLoggerService, TopLoggerService>();
        builder.Services.AddRefitClient<ITopLoggerApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://api.toplogger.nu"));


        return builder.Build();
    }
}