using System.Text.Json;
using TopLoggerPlus.Contracts.Services.TopLogger;
using Gym = TopLoggerPlus.Contracts.Domain.Gym;
using User = TopLoggerPlus.Contracts.Domain.User;
using Route = TopLoggerPlus.Contracts.Domain.Route;
using Ascend = TopLoggerPlus.Contracts.Domain.Ascend;

namespace TopLoggerPlus.Contracts.Services;

public interface IRouteService
{
    Task<List<Gym>> GetGyms();
    Task<List<User>> GetUsers(int gymId);
    void SaveUserInfo(string gymName, long userUId);

    Task<(List<Route>? routes, DateTime syncTime)> GetRoutes();
    Route? GetRouteById(int routeId);
    Task<(List<Route>? routes, DateTime syncTime)> RefreshRoutes();

    void ClearAll();
}

public class RouteService : IRouteService
{
    private readonly ITopLoggerService _topLoggerService;
    private readonly string _gymFile;
    private readonly string _userFile;
    private readonly string _routesFile;

    public RouteService(ITopLoggerService topLoggerService)
    {
        _topLoggerService = topLoggerService;

        var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TopLoggerPlus");
        if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

        _gymFile = Path.Combine(directory, "gym.txt");
        _userFile = Path.Combine(directory, "user.txt");
        _routesFile = Path.Combine(directory, "routes.json");
    }

    public async Task<List<Gym>> GetGyms()
    {
        var gyms = await _topLoggerService.GetGyms();
        return gyms?.ConvertAll(g => new Gym { Id = g.Id, Name = g.Name }) ?? new List<Gym>();
    }
    public async Task<List<User>> GetUsers(int gymId)
    {
        var users = await _topLoggerService.GetUsers(gymId);
        return users?.ConvertAll(u => new User { Id = u.UId, Name = u.Name }) ?? new List<User>();
    }
    public void SaveUserInfo(string gymName, long userUId)
    {
        File.WriteAllText(_gymFile, gymName);
        File.WriteAllText(_userFile, userUId.ToString());
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
        if (!File.Exists(_gymFile) || !File.Exists(_userFile))
            return (null, DateTime.Now);

        var gymName = File.ReadAllText(_gymFile);
        if (!long.TryParse(File.ReadAllText(_userFile), out var userUId))
            return (null, DateTime.Now);

        var gymDetails = await _topLoggerService.GetGymByName(gymName);
        if (gymDetails == null) return (null, DateTime.Now);

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

    public void ClearAll()
    {
        if (File.Exists(_gymFile)) File.Delete(_gymFile);
        if (File.Exists(_userFile)) File.Delete(_userFile);
        if (File.Exists(_routesFile)) File.Delete(_routesFile);
    }
}
