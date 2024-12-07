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
    public static RouteColor GetRouteColor(this HoldColor? hold)
    {
        return hold != null
            ? new RouteColor { Name = hold.NameLoc, Value = hold.Color }
            : new RouteColor { Name = "unknown", Value = "#000000" };
    }
    
    // public static int GetGradeWithBonus(this Services.TopLogger.Ascend ascend, int grade)
    // {
    //     return ascend.TopType switch
    //     {
    //         RouteTopType.RedPoint => grade,
    //         RouteTopType.Flash => grade + 10,
    //         RouteTopType.OnSight => grade + 15,
    //         _ => 0,
    //     };
    // }
}
