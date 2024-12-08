namespace TopLoggerPlus.Contracts.Domain;

public class User
{
    public string Id { get; init; }
    public string Name { get; init; }
    
    public Gym Gym { get; init; }
    public List<Gym> FavoriteGyms { get; init; }
}

public class Gym
{
    public string Id { get; init; }
    public string Name { get; init; }

    public override string ToString()
    {
        return Name;
    }
}  