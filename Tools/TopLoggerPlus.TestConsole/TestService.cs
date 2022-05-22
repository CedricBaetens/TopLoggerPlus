namespace TopLoggerPlus.TestConsole;

public interface ITestService
{
    Task Run();
}

public class TestService : ITestService
{
    private readonly ILogger<TestService> _logger;
    private readonly ITopLoggerService _topLoggerService;
    private readonly IRouteService _routeService;

    public TestService(ILogger<TestService> logger, ITopLoggerService topLoggerService, IRouteService routeService)
    {
        _logger = logger;
        _topLoggerService = topLoggerService;
        _routeService = routeService;
    }

    public async Task Run()
    {
        //await TopLoggerServiceTests("klimax", 5437061749);
        await RouteServiceTests("klimax", 5437061749);
    }
    private async Task TopLoggerServiceTests(string gymName, long userUId)
    {
        var gyms = await _topLoggerService.GetGyms();
        _logger.LogDebug("Gyms retrieved; {0}", gyms);

        var gymDetails = await _topLoggerService.GetGymByName(gymName);
        _logger.LogDebug("Gym Details; {0}", gymDetails);

        if (gymDetails == null) return;
        var routes = await _topLoggerService.GetRoutes(gymDetails.Id);
        _logger.LogDebug("Routes; {0}", routes);
        var users = await _topLoggerService.GetUsers(gymDetails.Id);
        _logger.LogDebug("Users; {0}", users);

        var ascends = await _topLoggerService.GetAscends(userUId, gymDetails.Id);
        _logger.LogDebug("Ascends; {0}", ascends);
    }
    private async Task RouteServiceTests(string gymName, long userUId)
    {
        _routeService.SaveUserInfo(gymName, userUId);
        var routes = await _routeService.GetRoutes();
        _logger.LogDebug("Routes; {0}", routes);
    }
}
