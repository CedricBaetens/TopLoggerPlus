using System.Text.RegularExpressions;
using TopLoggerPlus.Contracts.Services.TopLogger;

namespace TopLoggerPlus.Contracts.Utils;

public static class ClimbExtensions
{
    public static int? GetGradeNumber(this string? input)
    {
        if (string.IsNullOrEmpty(input) || input == "2.0")
            return null;
        if (!Regex.IsMatch(input, "^\\d\\.\\d{1,2}$"))
            return null;

        var split = input.Split('.');
        return (int.Parse(split[0]) * 100) +
            split[1] switch
            {
                "0" or "00" => 0,
                "17" => 17,
                "33" => 33,
                "5" or "50" => 50,
                "67" => 67,
                "83" => 83,
                _ => 0,
            };
    }
    public static string GetFrenchGrade(this int? grade, bool detailed = false)
    {
        if (grade == null) return "?";

        var level = grade / 100;
        var letter = (grade % 100) switch
        {
            int n when (n < 9) => "a", //0
            int n when (n >= 9 && n < 25) => "a+", //17
            int n when (n >= 25 && n < 42) => "b", //33
            int n when (n >= 42 && n < 59) => "b+", //50
            int n when (n >= 59 && n < 75) => "c", //67
            int n when (n >= 75) => "c+", //83
            _ => ""
        };
        var details = detailed ? $" ({grade})" : "";

        return $"{level}{letter}{details}";
    }
    public static RouteColor GetRouteColor(this Hold? hold)
    {
        return hold != null
            ? new RouteColor { Name = hold.Brand, Value = hold.Color }
            : new RouteColor { Name = "unkonwn", Value = "#000000" };
    }
}
