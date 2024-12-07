namespace TopLoggerPlus.Contracts.Services.GraphQL;

public class Gym
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string NameSlug { get; set; }
    public string CountryCode { get; set; }
}

public class GymsResponse
{
    public List<Gym>? Gyms { get; set; }
}