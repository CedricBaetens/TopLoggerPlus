namespace TopLoggerPlus.Contracts.Services.GraphQL;

public class User
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
}

public class UsersResponse
{
    public List<User>? GymUsers { get; set; }
}