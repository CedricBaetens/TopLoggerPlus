namespace TopLoggerPlus.Contracts.Services.TopLogger.Requests;

public class SignInRequest
{
    [JsonPropertyName("user")]
    public AuthUser User { get; set; } = null!;

    public class AuthUser
    {
        [JsonPropertyName("email")]
        public string Email { get; set; } = null!;
        [JsonPropertyName("password")]
        public string Password { get; set; } = null!;
    }
}

public class SignInResponse
{
    [JsonPropertyName("user_id")]
    public int UserId { get; set; }
    [JsonPropertyName("authentication_token")]
    public string AuthenticationToken { get; set; } = null!;
}
