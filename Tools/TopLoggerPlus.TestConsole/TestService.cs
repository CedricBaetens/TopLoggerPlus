using TopLoggerPlus.Contracts.Services.GraphQL;
using Route = TopLoggerPlus.Contracts.Services.GraphQL.Route;

namespace TopLoggerPlus.TestConsole;

public interface ITestService
{
    Task Run();
}

public class TestService : ITestService
{
    private readonly ILogger<TestService> _logger;
    private readonly IAuthenticationService _authenticationService;
    private readonly IGraphQLService _graphQLService;

    public TestService(ILogger<TestService> logger, IAuthenticationService authenticationService, IGraphQLService graphQLService)
    {
        _logger = logger;
        _authenticationService = authenticationService;
        _graphQLService = graphQLService;
    }

    public async Task Run()
    {
        await GraphQLTest();
    }
    private async Task GraphQLTest()
    {
        // retrieve refresh token from dev tools in a browser
        // const string refreshToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJ4MWY0dHdicmFxNHo5OGVrNHNlOWkiLCJqdGkiOiJQUXBmZGpwVSIsImlhdCI6MTczMzU5ODYyNiwiZXhwIjoxNzM0ODA4MjI2fQ.0MxY1vh5MRJkvA4UOMfus_Ss4-FHOhy9BzjrR-TcWRI";
        // var accessToken = await _authenticationService.RefreshAccessToken(refreshToken);
        // _logger.LogInformation("AccessToken refreshed: {accessToken}", accessToken);
        
        var accessToken = await _authenticationService.GetAccessToken();
        _logger.LogInformation("AccessToken: {accessToken}", accessToken);
        
        var user = await _graphQLService.GetMyUserInfo();
        _logger.LogInformation($"Id: {user.Id}, Name: {user.FullName}, GymId: {user.Gym.Id}");
        
        // var gyms = await _graphQLService.GetGyms();
        // foreach (var gym in gyms?.ToList())
        //     _logger.LogInformation($"Name: {gym.Name}");

        // var routes = await _graphQLService.GetRoutes("b9i9x3bqtdd6um275gbje");
        // foreach (var route in routes ?? new List<Route>())
        //     _logger.LogInformation($"Id: {route.Id} Wall: {route.Wall?.NameLoc ?? "unknown"}");
    }
}
