namespace TopLoggerPlus.TestConsole.Utils;

public class TestConsoleRunner(
    IConfiguration configuration,
    ITestService testService,
    IHostApplicationLifetime lifetime)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (configuration.GetValue<string>("run") != "false")
            await testService.Run();
        if (configuration.GetValue<string>("wait") != "true")
            lifetime.StopApplication();
    }
}
