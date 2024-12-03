namespace TopLoggerPlus.Contracts.Services.GraphQL;

public class Gym
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string CountryCode { get; set; } = null!;
}

public class GymsResponse
{
    public List<Gym>? Gyms { get; set; }
}