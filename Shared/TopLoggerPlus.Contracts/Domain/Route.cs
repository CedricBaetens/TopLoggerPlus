namespace TopLoggerPlus.Contracts.Domain;

public class Route
{
    public int Id { get; set; }
    public string Grade { get; set; } = null!;
    public string GradeNumber { get; set; } = null!;
    public string Rope { get; set; } = null!;
    public string Wall { get; set; } = null!;
    public RouteColor Color { get; set; } = null!;
    public List<Ascend> Ascends { get; set; } = new List<Ascend>();

    public override string ToString()
    {
        return $"{Grade} - Rope {Rope} {Wall} - Ascends: {Ascends.Count}";
    }
}
