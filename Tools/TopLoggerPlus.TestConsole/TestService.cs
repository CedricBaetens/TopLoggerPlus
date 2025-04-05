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
        const string refreshToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJ4MWY0dHdicmFxNHo5OGVrNHNlOWkiLCJqdGkiOiJtcnFUS0s1eiIsImlhdCI6MTc0Mzg2ODQyNywiZXhwIjoxNzQ1MDc4MDI3fQ.pojCvzBHv4jz2LZxQXVPodFXYShmDPCEx2LmdpYAFro";
        var accessToken = await authenticationService.RefreshAccessToken(refreshToken);
        logger.LogInformation("AccessToken refreshed: {accessToken}", accessToken);
        
        // var accessToken = await authenticationService.GetAccessToken();
        // logger.LogInformation("AccessToken: {accessToken}", accessToken);
        
        var user = await graphQLService.GetMyUserInfo();
        logger.LogInformation($"Id: {user.Id}, Name: {user.FullName}, GymId: {user.Gym.Id}");

        var climbs = await graphQLService.GetClimbs(user.Gym.Id, user.Id);
        foreach (var route in climbs ?? [])
            logger.LogInformation($"{route.Wall?.NameLoc ?? "unknown"}|{route.HoldColor}|{route.Label}");
    }
}
