using Serilog;
using TopLoggerPlus.Contracts.Services.GraphQL;

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
        services.AddTransient<IGraphQLService, GraphQLService>();
        services.AddTransient<IAuthenticationService, AuthenticationService>();
        services.AddTransient<IStorageService, StorageService>();
    })
    .Build();

await host.RunAsync();