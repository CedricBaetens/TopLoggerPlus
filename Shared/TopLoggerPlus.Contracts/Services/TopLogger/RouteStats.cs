namespace TopLoggerPlus.Contracts.Services.TopLogger;

public class RouteStats
{
    [JsonPropertyName("community_grades")]
    public List<CommunityGrade> CommunityGrades { get; set; } = new List<CommunityGrade>();
    [JsonPropertyName("community_opinions")]
    public List<CommunityOpinion> CommunityOpinions { get; set; } = new List<CommunityOpinion>();
    [JsonPropertyName("toppers")]
    public List<Topper> Toppers { get; set; } = new List<Topper>();
}

public class CommunityGrade
{
    [JsonPropertyName("grade")]
    public string Grade { get; set; } = null!;
    [JsonPropertyName("count")]
    public int Count { get; set; }
}
public class CommunityOpinion
{
    [JsonPropertyName("stars")]
    public int Stars { get; set; }
    [JsonPropertyName("votes")]
    public int Votes { get; set; }
}
public class Topper
{
    [JsonPropertyName("uid")]
    public long UId { get; set; }
    [JsonPropertyName("full_name")]
    public string Name { get; set; } = null!;
    [JsonPropertyName("date_logged")]
    public DateTime DateLogged { get; set; }
    [JsonPropertyName("checks")]
    public RouteTopType TopType { get; set; }

    public override string ToString()
    {
        return $"{Name} - {TopType} - {DateLogged:d}";
    }
}