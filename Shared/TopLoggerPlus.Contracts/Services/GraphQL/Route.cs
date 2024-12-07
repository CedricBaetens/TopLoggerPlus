namespace TopLoggerPlus.Contracts.Services.GraphQL;

public class Route
{
    public string Id { get; set; }
    public int Grade { get; set; }
    
    public Wall? Wall { get; set; }
    public HoldColor? HoldColor { get; set; }
    
    public DateTime? InAt { get; set; }
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

public class PagedRoutes
{
    public List<Route>? Data { get; set; }
}
public class RoutesResponse
{
    public PagedRoutes? Climbs { get; set; }
}