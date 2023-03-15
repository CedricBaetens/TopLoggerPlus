namespace TopLoggerPlus.Contracts.Utils;

public static class TopLoggerExtensions
{
    public static int GetGradeWithBonus(this Services.TopLogger.Ascend ascend, int grade)
    {
        return ascend.TopType switch
        {
            RouteTopType.RedPoint => grade,
            RouteTopType.Flash => grade + 17,
            RouteTopType.OnSight => grade + 33,
            _ => 0,
        };
    }
}
