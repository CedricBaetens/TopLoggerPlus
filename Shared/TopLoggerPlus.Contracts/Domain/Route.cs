namespace TopLoggerPlus.Contracts.Domain;

public class Route
{
    public string Id { get; set; }
    public string Grade { get; set; }
    public int? GradeNumber { get; set; }
    public string Rope { get; set; }
    public string Wall { get; set; }
    public RouteColor Color { get; set; }
    public string Setter { get; set; }

    public DateTime? InAt { get; set; }
    public DateTime? OutPlannedAt { get; set; }
    public DateTime? OutAt { get; set; }

    public AscendsInfo AscendsInfo { get; set; }
    
    public string Top10String { get; set; }

    public override string ToString()
    {
        if (AscendsInfo is { MyGradeNumber: not null } && AscendsInfo.MyGradeNumber != GradeNumber)
            return $"{Grade} (My grade: {AscendsInfo.MyGrade}) - Rope {Rope} {Wall} - Tries: {AscendsInfo?.TotalTries ?? 0}";
        return $"{Grade} - Rope {Rope} {Wall} - Tries: {AscendsInfo?.TotalTries ?? 0}";
    }
}

public class RouteColor
{
    public string Name { get; set; }
    public string Value { get; set; }
}
public class AscendsInfo
{
    public string MyGrade { get; set; }
    public int? MyGradeNumber { get; set; }
    public int? Score { get; set; }
    
    public int TotalTries { get; set; }
    public RouteTopType TopType { get; set; }
    public DateTime TriedFirstAt { get; set; }
    public DateTime? ToppedFirstAt { get; set; }
}