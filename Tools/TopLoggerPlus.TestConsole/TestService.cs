using Microsoft.Extensions.Logging;
using TopLoggerPlus.Contracts.Services.TopLogger.Requests;

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
        await TopLoggerAuthTests("toplogger_testuser@mailinator.com", "password");
        //await TopLoggerServiceTests("klimax", 5437061749);
        //await RouteServiceTests(49, "klimax", 5437061749, 267453);
    }

    private async Task TopLoggerAuthTests(string userEmail, string userPassword)
    {
        var signInResponse = await _topLoggerService.SignIn(userEmail, userPassword);
        _logger.LogDebug("Auth Response; {0}", signInResponse);

        if (signInResponse == null)
        {
            _logger.LogDebug("Authentication failed");
            return;
        }

        var ascends1 = await _topLoggerService.GetAscends(1273301533, 49, userEmail, signInResponse.AuthenticationToken);
        _logger.LogDebug("Ascends; {0}", ascends1.Count);

        //var newAscend = new NewAscend
        //{
        //    UserId = signInResponse.UserId,
        //    ClimbId = 278061,
        //    TopType = (int) Contracts.Enums.RouteTopType.RedPoint,
        //    Topped = true,
        //    DateLogged = DateTime.UtcNow
        //};
        //var loggedAscend = await _topLoggerService.CreateAscends(new List<NewAscend> { newAscend }, userEmail, signInResponse.AuthenticationToken);
        //_logger.LogDebug("Logged Ascends; {0}", loggedAscend.Count);

        //var ascends2 = await _topLoggerService.GetAscends(1273301533, 49, userEmail, signInResponse.AuthenticationToken);
        //_logger.LogDebug("Ascends; {0}", ascends2.Count);
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
