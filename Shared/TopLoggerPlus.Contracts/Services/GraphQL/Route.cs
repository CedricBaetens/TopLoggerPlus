namespace TopLoggerPlus.Contracts.Services.GraphQL;

public class Route
{
    public string Id { get; set; } = null!;
    public int Grade { get; set; }
    
    public Wall? Wall { get; set; }
    public HoldColor? HoldColor { get; set; }
    
    public DateTime? InAt { get; set; }
    public DateTime? OutAt { get; set; }
}
public class Wall
{
    public string NameLoc { get; set; } = null!;
}
public class HoldColor
{
    public string Color { get; set; } = null!;
    public string NameLoc { get; set; } = null!;
}

public class RoutesResponse
{
    public PagedRoutes? Climbs { get; set; }
}
public class PagedRoutes
{
    public List<Route>? Data { get; set; }
}