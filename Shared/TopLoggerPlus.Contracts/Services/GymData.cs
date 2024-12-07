namespace TopLoggerPlus.Contracts.Services;

public class GymData
{
    public string UserId { get; set; }
    public List<GraphQL.Route> Routes { get; init; }
    //public List<TopLogger.Ascend>? Ascends { get; set; }
}
