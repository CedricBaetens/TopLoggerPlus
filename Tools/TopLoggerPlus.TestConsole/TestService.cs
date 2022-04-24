namespace TopLoggerPlus.TestConsole;

public interface ITestService
{
    Task Run();
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

    public async Task Run()
    {
        var gyms = await _topLoggerService.GetGyms();
        _logger.LogDebug("Gyms retrieved; {0}", gyms);

        var gymDetails = await _topLoggerService.GetGymByName("klimax");
        _logger.LogDebug("Gym Details; {0}", gymDetails);

        if (gymDetails == null) return;
        var routes = await _topLoggerService.GetRoutes(gymDetails.Id);
        _logger.LogDebug("Routes; {0}", routes);
        var users = await _topLoggerService.GetUsers(gymDetails.Id);
        _logger.LogDebug("Users; {0}", users);

        var ascends = await _topLoggerService.GetAscends(5437061749, gymDetails.Id);
        _logger.LogDebug("Ascends; {0}", ascends);
    }
}
