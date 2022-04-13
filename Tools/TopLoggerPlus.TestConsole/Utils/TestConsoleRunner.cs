namespace TopLoggerPlus.TestConsole.Utils;

public class TestConsoleRunner : BackgroundService
{
    private readonly ITestService _testService;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly IConfiguration _configuration;

    public TestConsoleRunner(IConfiguration configuration, ITestService testService, IHostApplicationLifetime lifetime)
    {
        _configuration = configuration;
        _testService = testService;
        _lifetime = lifetime;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_configuration.GetValue<string>("run") != "false")
            _testService.Run();

        if (_configuration.GetValue<string>("wait") != "true")
            _lifetime.StopApplication();

        return Task.CompletedTask;
    }
}
