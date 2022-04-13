using System;
using System.Collections.Generic;

namespace TopLoggerPlus.Logic.Model
{
    public class Route
    {
        public int Id { get; set; }
        public string Grade { get; set; }
        public string GradeNumber { get; set; }
        public string Rope { get; set; }
        public string Wall { get; set; }
        public RouteColor Color { get; set; }
        public List<Ascend> Ascends { get; set; } = new List<Ascend>();
    }
    public class Ascend
    {
        public DateTime LoggedAt { get; set; }
        public RouteTopType TopType { get; set; }
    }

    public enum RouteTopType
    {
        NotTopped = 0,
        RedPoint = 1,
        Flash = 2,
        OnSight = 3
    }
}
