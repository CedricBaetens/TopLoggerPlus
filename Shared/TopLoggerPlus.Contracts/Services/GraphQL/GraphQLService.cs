using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Newtonsoft.Json;

namespace TopLoggerPlus.Contracts.Services.GraphQL;

public interface IGraphQLService
{
    Task<User> GetMyUserInfo();
    Task<List<Gym>> GetGyms();
    Task<List<Route>> GetRoutes(string gymId);
}
public class GraphQLService : IGraphQLService
{
    private readonly IAuthenticationService _authenticationService;

    public GraphQLService(IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }

    public async Task<User> GetMyUserInfo()
    {
        var userMeRequest = new GraphQLRequest
        {
            OperationName = "userMeStore",
            Query = """
                    query userMeStore {
                      userMe {
                        ...userMeStore
                        __typename
                      }
                    }

                    fragment userMeStoreFavorite on GymUserMe {
                      id
                      gym {
                        id
                        name
                        nameSlug
                        iconPath
                        __typename
                      }
                      __typename
                    }

                    fragment userMeStore on UserMe {
                      id
                      locale
                      gradingSystemRoutes
                      gradingSystemBoulders
                      anonymous
                      profileReviewed
                      avatarUploadPath
                      firstName
                      lastName
                      fullName
                      gender
                      email
                      gym {
                        id
                        nameSlug
                        __typename
                      }
                      gymUserFavorites {
                        ...userMeStoreFavorite
                        __typename
                      }
                      __typename
                    }
                    """
        };
        var result = await SendGraphQLQueryAsync<UserMeResponse>(userMeRequest);
        return result.UserMe;
    }
    public async Task<List<Gym>> GetGyms()
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
    public async Task<List<Route>> GetRoutes(string gymId)
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
        
        var result = await SendGraphQLQueryAsync<RoutesResponse>(gymsRequest);
        return result?.Climbs?.Data;
    }
    
    private async Task<T> SendGraphQLQueryAsync<T>(GraphQLRequest request)
    {
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {await _authenticationService.GetAccessToken()}");
        
        using var graphQLClient = new GraphQLHttpClient("https://app.toplogger.nu/graphql", new NewtonsoftJsonSerializer(), httpClient);
        var response = await graphQLClient.SendQueryAsync<T>(request);
        if (response.Errors == null)
            return response.Data;
        
        var message = JsonConvert.SerializeObject(response.Errors, Formatting.Indented);
        if (message.Contains("UNAUTHENTICATED"))
            throw new AuthenticationFailedException(message);
        throw new GraphQLFailedException(message);
    }
}