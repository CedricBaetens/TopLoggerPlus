using TopLoggerPlus.Contracts.Services.TopLogger;

namespace TopLoggerPlus.Contracts.Services;

public class GymData
{
    public long? UserUId { get; set; }
    public List<GraphQL.Route>? Routes { get; init; }
    public List<TopLogger.Ascend>? Ascends { get; set; }
}
