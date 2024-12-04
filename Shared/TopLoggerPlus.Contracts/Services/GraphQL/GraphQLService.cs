using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Newtonsoft.Json;

namespace TopLoggerPlus.Contracts.Services.GraphQL;

public interface IGraphQLService
{
    Task<AuthTokens?> GetTokens(string refreshToken);
    
    Task<List<Gym>?> GetGyms();
    Task<List<Route>?> GetRoutes(string gymId);
}
public class GraphQLService : IGraphQLService
{
    private readonly IStorageService _storageService;

    public GraphQLService(IStorageService storageService)
    {
        _storageService = storageService;
    }

    public async Task<AuthTokens?> GetTokens(string refreshToken)
    {
        var tokensRequest = new GraphQLRequest
        {
            OperationName = "authSigninRefreshToken",
            Variables = new { refreshToken },
            Query = """
                    mutation authSigninRefreshToken($refreshToken: JWT!) {
                      tokens: authSigninRefreshToken(refreshToken: $refreshToken) {
                        ...authTokens
                        __typename
                      }
                    }

                    fragment authTokens on AuthTokens {
                      access {
                        token
                        expiresAt
                        __typename
                      }
                      refresh {
                        token
                        expiresAt
                        __typename
                      }
                      __typename
                    }
                    """
        };
        var result = await SendGraphQLQueryAsync<TokensResponse?>(tokensRequest, refreshToken);
        return result?.Tokens;
    }
    
    public async Task<List<Gym>?> GetGyms()
    {
        var gymsRequest = new GraphQLRequest
        {
            OperationName = "gyms",
            Query = """
                    query gyms {
                      gyms {
                        ...gymsItem
                        __typename
                      }
                    }

                    fragment gymsItem on Gym {
                      id
                      name
                      countryCode
                      __typename
                    }
                    """
        };
        var result = await SendGraphQLQueryAsync<GymsResponse>(gymsRequest);
        return result.Gyms;
    }
    public async Task<List<Route>?> GetRoutes(string gymId)
    {
        var gymsRequest = new GraphQLRequest
        {
            OperationName = "climbs",
            Variables = new { gymId, climbType = "route" },
            Query = """
                    query climbs($gymId: ID!, $climbType: ClimbType!) {
                      climbs(
                        gymId: $gymId
                        climbType: $climbType
                      ) {
                        data {
                          ...climb
                          __typename
                        }
                        __typename
                      }
                    }

                    fragment climb on Climb {
                      id
                      climbType
                      positionX
                      positionY
                      gradeAuto
                      grade
                      gradeVotesCount
                      gradeUsersVsAdmin
                      picPath
                      label
                      name
                      zones
                      remarksLoc
                      suitableForKids
                      clips
                      holds
                      height
                      overhang
                      leadEnabled
                      leadRequired
                      ratingsAverage
                      ticksCount
                      inAt
                      outAt
                      outPlannedAt
                      order
                      setterName
                      climbSetters {
                        id
                        gymAdmin {
                          id
                          name
                          picPath
                          __typename
                        }
                        __typename
                      }
                      wallId
                      wall {
                        id
                        nameLoc
                        labelX
                        labelY
                        __typename
                      }
                      wallSectionId
                      wallSection {
                        id
                        name
                        routesEnabled
                        positionX
                        positionY
                        __typename
                      }
                      holdColorId
                      holdColor {
                        id
                        color
                        colorSecondary
                        nameLoc
                        order
                        __typename
                      }
                      __typename
                    }
                    """
        };
        // var result = await SendGraphQLQueryAsync<object>(gymsRequest);
        // return Array.Empty<Route>();
        
        var result = await SendGraphQLQueryAsync<RoutesResponse?>(gymsRequest);
        return result?.Climbs?.Data;
    }
    
    private async Task<T> SendGraphQLQueryAsync<T>(GraphQLRequest request, string? authorizationToken = null)
    {
        using var httpClient = new HttpClient();
        if (authorizationToken != null)
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authorizationToken}");
        
        using var graphQLClient = new GraphQLHttpClient("https://app.toplogger.nu/graphql", new NewtonsoftJsonSerializer(), httpClient);
        var response = await graphQLClient.SendQueryAsync<T>(request);
        //File.WriteAllText("d:\\data.json", JsonConvert.SerializeObject(response.Data, Formatting.Indented));
        if (response.Errors != null) 
            Console.WriteLine($"Request failed: {request.OperationName}, Error: {response.Errors.FirstOrDefault()?.Message}");
        return response.Data;
    }
}