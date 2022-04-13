namespace TopLoggerPlus.TestConsole;

public interface ITestService
{
    void Run();
}

public class TestService : ITestService
{
    private readonly ILogger<TestService> _logger;
    private readonly ITopLoggerService _topLoggerService;

    public TestService(ILogger<TestService> logger, ITopLoggerService topLoggerService)
    {
        _logger = logger;
        _topLoggerService = topLoggerService;
    }

    public void Run()
    {
        //var gyms = _topLoggerService.GetGyms();
        //_logger.LogDebug("Gyms retrieved; {0}", gyms);
        var gymDetails = _topLoggerService.GetGymByName("klimax");
        _logger.LogDebug("Gym Details; {0}", gymDetails);
    }
}
