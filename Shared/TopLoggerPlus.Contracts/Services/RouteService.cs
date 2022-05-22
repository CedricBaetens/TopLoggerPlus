using System.Text.Json;
using TopLoggerPlus.Contracts.Services.TopLogger;
using Ascend = TopLoggerPlus.Contracts.Domain.Ascend;
using Route = TopLoggerPlus.Contracts.Domain.Route;

namespace TopLoggerPlus.Contracts.Services;

public interface IRouteService
{
    Task<(List<Route>? routes, DateTime syncTime)> GetRoutes();
    Route? GetRouteById(int routeId);

    Task<(List<Route>? routes, DateTime syncTime)> RefreshRoutes();
}

public class RouteService : IRouteService
{
    private readonly string _gymName = "klimax";
    private readonly long _userUId = 5437061749;

    private readonly ITopLoggerService _topLoggerService;
    private readonly string _routesFile;

    public RouteService(ITopLoggerService topLoggerService)
    {
        _topLoggerService = topLoggerService;

        var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TopLoggerPlus");
        if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

        _routesFile = Path.Combine(directory, "routes.json");
    }

    public async Task<(List<Route>? routes, DateTime syncTime)> GetRoutes()
    {
        if (File.Exists(_routesFile))
        {
            var routes = JsonSerializer.Deserialize<List<Route>>(File.ReadAllText(_routesFile));
            return (routes, File.GetLastWriteTime(_routesFile));
        }

        return await RefreshRoutes();
    }
    public Route? GetRouteById(int routeId)
    {
        if (!File.Exists(_routesFile)) return null;

        var routes = JsonSerializer.Deserialize<List<Route>>(File.ReadAllText(_routesFile));
        return routes?.FirstOrDefault(r => r.Id == routeId);
    }

    public async Task<(List<Route>? routes, DateTime syncTime)> RefreshRoutes()
    {
        var gymDetails = await _topLoggerService.GetGymByName(_gymName);
        if (gymDetails == null) return (null, DateTime.Now);

        var routesTask = _topLoggerService.GetRoutes(gymDetails.Id);
        var ascendsTask = _topLoggerService.GetAscends(_userUId, gymDetails.Id);

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
                Grade = apiRoute.Grade.GetFrenchGrade(),
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

        File.WriteAllText(_routesFile, JsonSerializer.Serialize(result));
        return (result, DateTime.Now);
    }
}
