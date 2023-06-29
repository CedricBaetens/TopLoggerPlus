namespace TopLoggerPlus.Contracts.Services.TopLogger;

public class Route
{
    [JsonPropertyName("id")]
    public long Id { get; set; }
    [JsonPropertyName("gym_id")]
    public long GymId { get; set; }

    [JsonPropertyName("grade")]
    public string Grade { get; set; } = null!;
    [JsonPropertyName("rope_number")]
    public string? RopeNumber { get; set; }
    [JsonPropertyName("wall_id")]
    public int WallId { get; set; }
    [JsonPropertyName("setter_id")]
    public int SetterId { get; set; }
    [JsonPropertyName("hold_id")]
    public int HoldId { get; set; }

    [JsonPropertyName("live")]
    public bool Live { get; set; }
    [JsonPropertyName("deleted")]
    public bool Deleted { get; set; }

    [JsonPropertyName("date_set")]
    public DateTime DateSet { get; set; }
    [JsonPropertyName("date_live_start")]
    public DateTime? LiveStarted { get; set; }
    [JsonPropertyName("date_live_end")]
    public DateTime? LiveEnded { get; set; }

    public override string ToString()
    {
        return $"{Grade} - Rope {RopeNumber}";
    }
}
