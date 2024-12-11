using TopLoggerPlus.Contracts.Services.GraphQL;

namespace TopLoggerPlus.Contracts.Utils;

public static class ClimbExtensions
{
    public static string GetFrenchGrade(this int grade, bool detailed = false)
    {
        if (grade == 0) return "?";
        
        var level = grade / 100;
        var letter = (grade % 100) switch
        {
            < 9 => "a", //0
            < 25 => "a+", //17
            < 42 => "b", //33
            < 59 => "b+", //50
            < 75 => "c", //67
            >= 75 => "c+", //83
        };
        var details = detailed ? $" ({grade})" : "";

        return $"{level}{letter}{details}";
    }
    public static string GetRopeNumber(this Climb climb, List<RopeNumber> ropeNumbers)
    {
        if (climb.Grade == 0) return "/";
        
        var ropeNumber = ropeNumbers
            .Where(n => string.Equals(n.Wall, climb.Wall?.NameLoc, StringComparison.CurrentCultureIgnoreCase)
                                    && string.Equals(n.Color, climb.HoldColor?.NameLoc, StringComparison.CurrentCultureIgnoreCase))
            .OrderBy(n => Math.Abs(n.Grade - climb.Grade))
            .FirstOrDefault();
        return ropeNumber?.Number.ToString() ?? "/";
    }
    public static RouteColor GetRouteColor(this HoldColor hold)
    {
        return hold != null
            ? new RouteColor { Name = hold.NameLoc, Value = hold.Color }
            : new RouteColor { Name = "unknown", Value = "#000000" };
    }
    public static int? GetGradeWithBonus(this ClimbUser ascend, int grade)
    {
        return ascend.TickType switch
        {
            RouteTopType.NotTopped => null,
            RouteTopType.RedPoint => grade,
            RouteTopType.Flash => grade + 10,
            RouteTopType.OnSight => grade + 15,
            _ => null
        };
    }
}
