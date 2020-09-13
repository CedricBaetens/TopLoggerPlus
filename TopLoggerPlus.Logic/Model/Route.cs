namespace TopLoggerPlus.Logic.Model
{
    public class Route
    {
        public string Grade { get; set; }
        public string GradeNumber { get; set; }
        public string Rope { get; set; }
        public string Wall { get; set; }
        public RouteTopType TopType { get; set; }
        public RouteColor Color { get; set; }
    }

    public enum RouteTopType
    {
        NotTopped,
        OnSight,
        Flash,
        RedPoint
    }

}
