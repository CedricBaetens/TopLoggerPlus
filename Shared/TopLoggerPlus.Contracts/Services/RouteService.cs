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
    void SaveUserInfo(Gym gym, User user);

    Task<(List<Route>? routes, DateTime syncTime)> GetRoutes();
    Route? GetRouteById(int routeId);
    Task<(List<Route>? routes, DateTime syncTime)> RefreshRoutes();

    Task<RouteCommunityInfo?> GetRouteCommunityInfo(int routeId);

    void ClearAll();
}

public class RouteService : IRouteService
{
    private readonly ITopLoggerService _topLoggerService;
    private readonly string _gymIdFile;
    private readonly string _gymNameFile;
    private readonly string _userIdFile;
    private readonly string _routesFile;

    public RouteService(ITopLoggerService topLoggerService)
    {
        _topLoggerService = topLoggerService;

        var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TopLoggerPlus");
        if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

        _gymIdFile = Path.Combine(directory, "gymid.txt");
        _gymNameFile = Path.Combine(directory, "gymname.txt");
        _userIdFile = Path.Combine(directory, "userid.txt");
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
    public void SaveUserInfo(Gym gym, User user)
    {
        File.WriteAllText(_gymIdFile, gym.Id.ToString());
        File.WriteAllText(_gymNameFile, gym.Name);
        File.WriteAllText(_userIdFile, user.Id.ToString());
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
        if (!File.Exists(_gymNameFile) || !File.Exists(_userIdFile))
            return (null, DateTime.Now);

        var gymName = File.ReadAllText(_gymNameFile);
        if (!long.TryParse(File.ReadAllText(_userIdFile), out var userUId))
            return (null, DateTime.Now);

        var gymDetails = await _topLoggerService.GetGymByName(gymName);
        if (gymDetails == null) return (null, DateTime.Now);

        var routesTask = _topLoggerService.GetRoutes(gymDetails.Id);
        var ascendsTask = _topLoggerService.GetAscends(userUId, gymDetails.Id);
        var opinionTask = _topLoggerService.GetOpinions(userUId, gymDetails.Id);

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

            // Opinion
            var opinion = (await opinionTask).Where(o => o.ClimbId == apiRoute.Id).FirstOrDefault();
            if (opinion != null)
            {
                route.MyGrade = opinion.Grade.GetFrenchGrade();
                route.MyGradeNumber = opinion.Grade;
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

    public async Task<RouteCommunityInfo?> GetRouteCommunityInfo(int routeId)
    {
        if (!File.Exists(_gymIdFile) || !int.TryParse(File.ReadAllText(_gymIdFile), out var gymId))
            return null;

        var routeStats = await _topLoggerService.GetRouteStats(gymId, routeId);
        if (routeStats == null) return null;

        var grades = routeStats.CommunityGrades?.OrderBy(g => g.Grade)
            .Select(g => $"{g.Grade.GetFrenchGrade()} ({g.Count} votes)")
            .ToList() ?? new List<string>();

        var stars = routeStats.CommunityOpinions?
            .Where(o => o.Votes > 0).OrderBy(o => o.Stars)
            .Select(o => $"{o.Stars} stars ({o.Votes} votes)")
            .ToList() ?? new List<string>();

        var toppers = new List<string>();
        foreach (var topper in routeStats.Toppers)
        {
            var topOpinion = (await _topLoggerService.GetOpinions(topper.UId, gymId, routeId)).FirstOrDefault();
            toppers.Add(topOpinion?.Grade == null ? topper.ToString() : $"{topper} (voted {topOpinion.Grade.GetFrenchGrade()})");
        }

        return new RouteCommunityInfo
        {
            CommunityGrades = grades.Any() ? string.Join(", ", grades) : "No tops",
            CommunityStars = stars.Any() ? string.Join(", ", stars) : "No stars",
            Toppers = toppers
        };
    }

    public void ClearAll()
    {
        if (File.Exists(_gymIdFile)) File.Delete(_gymIdFile);
        if (File.Exists(_gymNameFile)) File.Delete(_gymNameFile);
        if (File.Exists(_userIdFile)) File.Delete(_userIdFile);
        if (File.Exists(_routesFile)) File.Delete(_routesFile);
    }
}
