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
    private readonly IGraphQLService _graphQLService;

    public TestService(ILogger<TestService> logger, IGraphQLService graphQLService)
    {
        _logger = logger;
        _graphQLService = graphQLService;
    }

    public async Task Run()
    {
        await GraphQLTest();
    }
    private async Task GraphQLTest()
    {
        // var gyms = await _graphQLService.GetGyms();
        // foreach (var gym in gyms?.ToList())
        //     _logger.LogInformation($"Name: {gym.Name}");

        // var routes = await _graphQLService.GetRoutes("b9i9x3bqtdd6um275gbje");
        // foreach (var route in routes ?? new List<Route>())
        //     _logger.LogInformation($"Id: {route.Id} Wall: {route.Wall?.NameLoc ?? "unknown"}");
        
        var authTokens = await _graphQLService.GetTokens("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJ4MWY0dHdicmFxNHo5OGVrNHNlOWkiLCJqdGkiOiJ0a0xHaVo1bCIsImlhdCI6MTczMzM0ODU0OCwiZXhwIjoxNzM0NTU4MTQ4fQ.eVwn8ucbOlmLQIRsZyKFoSvFmbo3PMIPddorEKYcVfI");
        _logger.LogInformation("AccessToken: {accessToken}, RefreshToken: {refreshToken}",
            authTokens?.Access.Token, authTokens?.Refresh.Token);
    }
}
