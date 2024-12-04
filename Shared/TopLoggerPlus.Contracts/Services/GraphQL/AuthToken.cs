namespace TopLoggerPlus.Contracts.Services.GraphQL;

public class AuthToken
{
    public string Token { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
}

public class AuthTokens
{
    public AuthToken Access { get; set; } = null!;
    public AuthToken Refresh { get; set; } = null!;
}
public class TokensResponse
{
    public AuthTokens Tokens { get; set; } = null!;
}