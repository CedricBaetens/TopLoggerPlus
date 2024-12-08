namespace TopLoggerPlus.Contracts.Services.GraphQL;

public class AuthToken
{
    public string Token { get; set; }
    public DateTime ExpiresAt { get; set; }
}

public class AuthTokens
{
    public AuthToken Access { get; set; }
    public AuthToken Refresh { get; set; }
}
public class TokensResponse
{
    public AuthTokens Tokens { get; set; }
}