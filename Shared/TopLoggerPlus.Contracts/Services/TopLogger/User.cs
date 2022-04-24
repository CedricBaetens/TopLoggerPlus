using System.Text.Json.Serialization;

namespace TopLoggerPlus.Contracts.Services.TopLogger;

public class User
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    [JsonPropertyName("uid")]
    public long UId { get; set; }
    [JsonPropertyName("full_name")]
    public string Name { get; set; } = null!;
    [JsonPropertyName("gender")]
    public string Gender { get; set; } = null!;
    [JsonPropertyName("score")]
    public string Score { get; set; } = null!;
    [JsonPropertyName("score_count")]
    public int ScoreCount { get; set; }

    public override string ToString()
    {
        return $"{Name} - {Score}";
    }
}
