using System.Text.Json.Serialization;

namespace TopLoggerPlus.Contracts.Services.TopLogger;

public class Ascend
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    [JsonPropertyName("user_id")]
    public int UserId { get; set; }
    [JsonPropertyName("climb_id")]
    public int ClimbId { get; set; }
    [JsonPropertyName("topped")]
    public bool Topped { get; set; }
    [JsonPropertyName("date_logged")]
    public DateTime DateLogged { get; set; }
    [JsonPropertyName("used")]
    public bool Used { get; set; }
    [JsonPropertyName("checks")]
    public RouteTopType TopType { get; set; }

    public override string ToString()
    {
        return $"{TopType} - {DateLogged:d}";
    }
}