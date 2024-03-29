﻿namespace TopLoggerPlus.Contracts.Services.TopLogger;

public class Gym
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;
    public override string ToString()
    {
        return $"{Id} - {Name}";
    }
}

public class GymDetails : Gym
{
    [JsonPropertyName("holds")]
    public List<Hold> Holds { get; set; } = new List<Hold>();
    [JsonPropertyName("setters")]
    public List<Setter> Setters { get; set; } = new List<Setter>();
    [JsonPropertyName("walls")]
    public List<Wall> Walls { get; set; } = new List<Wall>();
}

public class Hold
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    [JsonPropertyName("color")]
    public string Color { get; set; } = null!;
    [JsonPropertyName("brand")]
    public string Brand { get; set; } = null!;
}
public class Setter
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    [JsonPropertyName("user_id")]
    public int? UserId { get; set; }
    [JsonPropertyName("email")]
    public string Email { get; set; } = null!;
    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;
    [JsonPropertyName("order")]
    public int Order { get; set; }
}
public class Wall
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;
}
