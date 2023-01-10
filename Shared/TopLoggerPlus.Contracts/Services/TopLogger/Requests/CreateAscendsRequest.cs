namespace TopLoggerPlus.Contracts.Services.TopLogger.Requests;

public class CreateAscendsRequest
{
    [JsonPropertyName("ascends")]
    public List<NewAscend> Ascends { get; set; } = null!;
}

public class NewAscend
{
    [JsonPropertyName("user_id")]
    public int UserId { get; set; }
    [JsonPropertyName("climb_id")]
    public int ClimbId { get; set; }
    [JsonPropertyName("topped")]
    public bool Topped { get; set; }
    [JsonPropertyName("date_logged")]
    public DateTime DateLogged { get; set; }
    [JsonPropertyName("checks")]
    public int TopType { get; set; }
}
