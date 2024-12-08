namespace TopLoggerPlus.Contracts.Domain;

public class RouteCommunityInfo
{
    public string CommunityGrades { get; set; }
    public string CommunityStars { get; set; }
    public List<User> Toppers { get; set; }
}