namespace TopLoggerPlus.Contracts.Services.GraphQL;

public class Climb
{
    public string Id { get; set; }
    public int Grade { get; set; }
    
    public Wall Wall { get; set; }
    public HoldColor HoldColor { get; set; }
    public ClimbSetter[] ClimbSetters { get; set; }
    
    public ClimbUser ClimbUser { get; set; }
    
    public DateTime? InAt { get; set; }
    public DateTime? OutPlannedAt { get; set; }
    public DateTime? OutAt { get; set; }
}
public class Wall
{
    public string NameLoc { get; set; }
}
public class HoldColor
{
    public string Color { get; set; }
    public string NameLoc { get; set; }
}

public class ClimbSetter
{
    public string Id { get; set; }
    public GymAdmin GymAdmin { get; set; }
}
public class GymAdmin
{
    public string Id { get; set; }
    public string Name { get; set; }
}

public class ClimbUser
{
    public string Id { get; set; }
    public int? Grade { get; set; }
    public RouteTopType TickType { get; set; }
    public int TotalTries { get; set; }
    public DateTime? TriedFirstAtDate { get; set; }
    public DateTime? TickedFirstAtDate { get; set; }
}

public class PagedClimbs
{
    public List<Climb> Data { get; set; }
}
public class ClimbsResponse
{
    public PagedClimbs Climbs { get; set; }
}