using Refit;
using Serilog;

var host = Host.CreateDefaultBuilder(args)
    .UseSerilog((context, configuration) =>
    {
        configuration.ReadFrom.Configuration(context.Configuration)
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} - {Message:lj}{NewLine}{Exception}")
            .Enrich.WithProperty("Application", "TopLoggerPlus.TestConsole").Enrich.FromLogContext();
    })
    .ConfigureServices(services =>
    {
        services.AddHostedService<TestConsoleRunner>();
        services.AddTransient<ITestService, TestService>();

        services.AddTransient<ITopLoggerService, TopLoggerService>();
        services.AddRefitClient<ITopLoggerApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://api.toplogger.nu"));

        services.AddTransient<IRouteService, RouteService>();
    })
    .Build();

await host.RunAsync();