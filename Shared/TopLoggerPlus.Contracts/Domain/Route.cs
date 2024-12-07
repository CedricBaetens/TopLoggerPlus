namespace TopLoggerPlus.Contracts.Domain;

public class Route
{
    public string Id { get; set; }
    public string Grade { get; set; }
    public int? GradeNumber { get; set; }
    public string Rope { get; set; }
    public string Wall { get; set; }
    public RouteColor Color { get; set; }

    public List<Ascend> Ascends { get; set; } = new List<Ascend>();
    public string? MyGrade { get; set; }
    public int? MyGradeNumber { get; set; }
    public int? BestAttemptScore { get; set; }
    public DateTime? BestAttemptDateLogged { get; set; }
    public string? Top10String { get; set; }

    public bool Live { get; set; }
    public bool Deleted { get; set; }

    public override string ToString()
    {
        if (MyGradeNumber.HasValue && MyGradeNumber != GradeNumber)
            return $"{Grade} (My grade: {MyGrade}) - Rope {Rope} {Wall} - Ascends: {Ascends.Count}";
        return $"{Grade} - Rope {Rope} {Wall} - Ascends: {Ascends.Count}";
    }
}
