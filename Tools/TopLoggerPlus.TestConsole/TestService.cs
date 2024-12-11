using TopLoggerPlus.Contracts.Services.GraphQL;

namespace TopLoggerPlus.TestConsole;

public interface ITestService
{
    Task Run();
}

public class TestService(
    ILogger<TestService> logger,
    IAuthenticationService authenticationService,
    IGraphQLService graphQLService)
    : ITestService
{
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
        
        var accessToken = await authenticationService.GetAccessToken();
        logger.LogInformation("AccessToken: {accessToken}", accessToken);
        
        var user = await graphQLService.GetMyUserInfo();
        logger.LogInformation($"Id: {user.Id}, Name: {user.FullName}, GymId: {user.Gym.Id}");

        var climbs = await graphQLService.GetClimbs(user.Gym.Id, user.Id);
        foreach (var route in climbs ?? [])
            logger.LogInformation($"{route.Wall?.NameLoc ?? "unknown"}|{route.HoldColor}");
    }
}
