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
        await RouteServiceTests(49, "klimax", 5437061749, 267453);
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
    private async Task RouteServiceTests(int gymId, string gymName, long userUId, int routeId)
    {
        var gym = new Contracts.Domain.Gym { Id = gymId, Name = gymName };
        var user = new Contracts.Domain.User { Id = userUId };

        _routeService.SaveUserInfo(gym, user);
        var routes = await _routeService.GetRoutes();
        _logger.LogDebug("Routes; {0}", routes);

        var routeCommunityInfo = await _routeService.GetRouteCommunityInfo(routeId);
        _logger.LogDebug("RouteCommunityInfo; {0}", routeCommunityInfo);
    }
}
