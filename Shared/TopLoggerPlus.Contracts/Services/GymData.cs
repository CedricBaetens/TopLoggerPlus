using TopLoggerPlus.Contracts.Services.TopLogger;

namespace TopLoggerPlus.Contracts.Services;

public class GymData
{
    public long? UserUId { get; set; }
    public GymDetails? GymDetails { get; set; }
    public List<TopLogger.Route>? Routes { get; set; }
    public List<TopLogger.Ascend>? Ascends { get; set; }
}
