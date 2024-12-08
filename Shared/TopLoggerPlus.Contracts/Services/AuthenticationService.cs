using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Newtonsoft.Json;
using TopLoggerPlus.Contracts.Services.GraphQL;

namespace TopLoggerPlus.Contracts.Services;

public interface IAuthenticationService
{
    Task<string> GetAccessToken();
    Task<string> RefreshAccessToken(string refreshToken);
    void Logout();
}

public class AuthenticationService : IAuthenticationService
{
    private readonly IStorageService _storageService;

    public AuthenticationService(IStorageService storageService)
    {
        _storageService = storageService;
    }

    public async Task<string> GetAccessToken()
    {
        var authTokens = _storageService.Read<AuthTokens>("AuthTokens");
        if (authTokens == null) throw new AuthenticationFailedException("No auth tokens found");

        if (authTokens.Access.ExpiresAt > DateTime.Now.AddMinutes(5))
            return authTokens.Access.Token;
        
        if (authTokens.Refresh.ExpiresAt < DateTime.Now.AddMinutes(5))
            throw new AuthenticationFailedException("Refresh token expired");
        
        return await RefreshAccessToken(authTokens.Refresh.Token);
    }
    public async Task<string> RefreshAccessToken(string refreshToken)
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
        
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {refreshToken}");
        
        using var graphQLClient = new GraphQLHttpClient("https://app.toplogger.nu/graphql", new NewtonsoftJsonSerializer(), httpClient);
        var response = await graphQLClient.SendQueryAsync<TokensResponse>(tokensRequest);
        if (response.Errors != null)
            throw new AuthenticationFailedException($"Authentication failed: {JsonConvert.SerializeObject(response.Errors, Formatting.Indented)}");

        _storageService.Write("AuthTokens", response.Data.Tokens);
        return response.Data.Tokens.Access.Token;
    }
    public void Logout()
    {
        _storageService.Delete("AuthTokens");
    }
}