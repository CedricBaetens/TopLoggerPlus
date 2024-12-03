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

        var routes = await _graphQLService.GetRoutes("b9i9x3bqtdd6um275gbje");
        foreach (var route in routes ?? new List<Route>())
            _logger.LogInformation($"Id: {route.Id} Wall: {route.Wall?.NameLoc ?? "unknown"}");
    }
}
