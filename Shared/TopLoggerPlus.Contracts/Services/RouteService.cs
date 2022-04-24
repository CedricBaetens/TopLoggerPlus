using TopLoggerPlus.Contracts.Services.TopLogger;
using Ascend = TopLoggerPlus.Contracts.Domain.Ascend;
using Route = TopLoggerPlus.Contracts.Domain.Route;

namespace TopLoggerPlus.Contracts.Services;

public interface IRouteService
{
    Task<List<Route>?> GetRoutes(string gymName, long userUId);
}

public class RouteService : IRouteService
{
    private readonly ITopLoggerService _topLoggerService;

    public RouteService(ITopLoggerService topLoggerService)
    {
        _topLoggerService = topLoggerService;
    }

    public async Task<List<Route>?> GetRoutes(string gymName, long userUId)
    {
        var gymDetails = await _topLoggerService.GetGymByName(gymName);
        if (gymDetails == null) return null;

        var routesTask = _topLoggerService.GetRoutes(gymDetails.Id);
        var ascendsTask = _topLoggerService.GetAscends(userUId, gymDetails.Id);

        var walls = gymDetails.Walls.ToDictionary(w => w.Id);
        var holds = gymDetails.Holds.ToDictionary(h => h.Id);

        var routes = (await routesTask)
            .Where(r => walls.ContainsKey(r.WallId))
            .ToList();

        var result = new List<Route>();
        foreach (var apiRoute in routes)
        {
            var route = new Route
            {
                Id = apiRoute.Id,
                Grade = GradeConvertor(apiRoute.Grade),
                GradeNumber = apiRoute.Grade,
                Rope = apiRoute.RopeNumber == 0 ? "/" : apiRoute.RopeNumber.ToString(),
                Wall = walls[apiRoute.WallId].Name
            };

            // Ascend
            foreach (var ascend in (await ascendsTask).Where(a => a.ClimbId == apiRoute.Id))
            {
                route.Ascends.Add(new Ascend
                {
                    LoggedAt = ascend.DateLogged,
                    TopType = ascend.TopType
                });
            }

            // Color
            var hold = holds[apiRoute.HoldId];
            route.Color = new RouteColor
            {
                Name = hold.Brand,
                Value = hold.Color
            };
            result.Add(route);
        }
        return result;
    }
    private string GradeConvertor(string input)
    {
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
