using System.Text.Json.Serialization;

namespace TopLoggerPlus.Contracts.Services.TopLogger;

public class Route
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("number")]
    public string Number { get; set; } = null!;
    [JsonPropertyName("grade")]
    public string Grade { get; set; } = null!;
    [JsonPropertyName("rope_number")]
    public int RopeNumber { get; set; }
    [JsonPropertyName("live")]
    public bool Live { get; set; }

    [JsonPropertyName("wall_id")]
    public int WallId { get; set; }
    [JsonPropertyName("setter_id")]
    public int SetterId { get; set; }
    [JsonPropertyName("hold_id")]
    public int HoldId { get; set; }

    public override string ToString()
    {
        return $"{Grade} - Rope {RopeNumber}";
    }
}
