﻿using TopLoggerPlus.App.Utils;
using TopLoggerPlus.Contracts.Services.GraphQL;

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

        builder.Services.AddTransient<AppShellViewModel>();

        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<LoginViewModel>();
        
        builder.Services.AddSingleton<AccountPage>();
        builder.Services.AddTransient<AccountViewModel>();

        builder.Services.AddSingleton<AllRoutesPage>();
        builder.Services.AddSingleton<ExpiringRoutesPage>();
        builder.Services.AddTransient<RouteOverviewViewModel>();

        builder.Services.AddSingleton<Top10Page>();
        builder.Services.AddTransient<RouteTop10ViewModel>();

        builder.Services.AddSingleton<RouteDetailsPage>();
        builder.Services.AddTransient<RouteDetailsViewModel>();

        builder.Services.AddTransient<IToploggerService, ToploggerService>();
        builder.Services.AddTransient<IGraphQLService, GraphQLService>();
        builder.Services.AddTransient<IAuthenticationService, AuthenticationService>();
        builder.Services.AddTransient<IStorageService, StorageService>();
        builder.Services.AddSingleton<IDialogService, DialogService>();

        return builder.Build();
    }
}