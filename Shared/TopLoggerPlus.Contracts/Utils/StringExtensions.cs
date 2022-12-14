namespace TopLoggerPlus.Contracts.Utils;

public static class StringExtensions
{
    public static string? GetFrenchGrade(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return null;

        if (input == "2.0")
            return "?";

        var split = input.Split('.');
        if (split.Length != 2)
            return input;

        var result = split[0];
        switch (split[1])
        {
            case "0":
                result += "a";
                break;

            case "17":
                result += "a+";
                break;

            case "33":
                result += "b";
                break;

            case "5":
                result += "b+";
                break;

            case "67":
                result += "c";
                break;

            case "83":
                result += "c+";
                break;
        }
        return result;
    }
}
