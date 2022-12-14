namespace TopLoggerPlus.Contracts.Services.TopLogger;

public class Opinion
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    [JsonPropertyName("user_id")]
    public int UserId { get; set; }
    [JsonPropertyName("climb_id")]
    public int ClimbId { get; set; }
    [JsonPropertyName("grade")]
    public string Grade { get; set; } = null!;
    [JsonPropertyName("project")]
    public bool Project { get; set; }
    [JsonPropertyName("vote_renew")]
    public bool VoteRenew { get; set; }
}