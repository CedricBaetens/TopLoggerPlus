namespace TopLoggerPlus.Contracts.Services.GraphQL;

public class User
{
    public string Id { get; set; }
    public string FullName { get; set; }
    public Gym Gym { get; set; }
    public List<GymUser> GymUserFavorites { get; set; }
}

public class Gym
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string NameSlug { get; set; }
    public string CountryCode { get; set; }
}

public class GymUser
{
    public string Id { get; set; }
    public Gym Gym { get; set; }
}

public class UserMeResponse
{
    public User UserMe { get; set; }
}