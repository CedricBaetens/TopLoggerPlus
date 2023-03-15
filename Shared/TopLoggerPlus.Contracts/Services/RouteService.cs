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

    Task<(List<Route>? routes, DateTime syncTime)> GetRoutes(bool refresh = false);
    Task<List<Route>?> GetBestAscends(int daysBack, bool refresh = false);
    Route? GetRouteById(int routeId);

    Task<RouteCommunityInfo?> GetRouteCommunityInfo(int routeId);

    void ClearAll();
}

public class RouteService : IRouteService
{
    private readonly ITopLoggerService _topLoggerService;
    private readonly string _gymIdFile;
    private readonly string _gymNameFile;
    private readonly string _userIdFile;
    private readonly string _gymData;
    private readonly string _processedRoutes;

    public RouteService(ITopLoggerService topLoggerService)
    {
        _topLoggerService = topLoggerService;

        var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TopLoggerPlus");
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        var dataVersionFile = Path.Combine(directory, "v0.txt");
        if (!File.Exists(dataVersionFile))
        {
            Directory.Delete(directory, true);
            Directory.CreateDirectory(directory);
            File.WriteAllText(dataVersionFile, "");
        }

        _gymIdFile = Path.Combine(directory, "gymid.txt");
        _gymNameFile = Path.Combine(directory, "gymname.txt");
        _userIdFile = Path.Combine(directory, "userid.txt");
        _gymData = Path.Combine(directory, "gymdata.json");
        _processedRoutes = Path.Combine(directory, "processedroutes.json");
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

    public async Task<(List<Route>? routes, DateTime syncTime)> GetRoutes(bool refresh = false)
    {
        var gymData = await GetGymData(refresh);
        if (gymData?.UserUId == null || gymData?.GymDetails == null || gymData?.Routes == null || gymData?.Ascends == null)
            return (null, DateTime.Now);

        var walls = gymData.GymDetails.Walls.ToDictionary(w => w.Id);
        var holds = gymData.GymDetails.Holds.ToDictionary(h => h.Id);

        var routes = gymData.Routes
            .Where(r => walls.ContainsKey(r.WallId))
            .ToList();

        var opinionTask = _topLoggerService.GetOpinions(gymData.UserUId.Value, gymData.GymDetails.Id);

        var processedRoutes = new List<Route>();
        foreach (var apiRoute in routes)
        {
            var route = new Route
            {
                Id = apiRoute.Id,
                Grade = apiRoute.Grade.GetGradeNumber().GetFrenchGrade(),
                GradeNumber = apiRoute.Grade.GetGradeNumber(),
                Rope = apiRoute.RopeNumber == 0 ? "/" : apiRoute.RopeNumber.ToString(),
                Wall = walls[apiRoute.WallId].Name,
                Color = holds[apiRoute.HoldId].GetRouteColor(),
                Live = apiRoute.Live,
                Deleted = apiRoute.Deleted
            };

            // Ascend
            foreach (var ascend in gymData.Ascends.Where(a => a.ClimbId == apiRoute.Id))
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
                route.MyGrade = opinion.Grade.GetGradeNumber().GetFrenchGrade();
                route.MyGradeNumber = opinion.Grade.GetGradeNumber();
            }

            processedRoutes.Add(route);
        }

        File.WriteAllText(_processedRoutes, JsonSerializer.Serialize(processedRoutes));
        return (processedRoutes.Where(r => !r.Deleted).ToList(), DateTime.Now);
    }
    public async Task<List<Route>?> GetBestAscends(int daysBack, bool refresh = false)
    {
        var gymData = await GetGymData(refresh);
        if (gymData?.UserUId == null || gymData?.GymDetails == null || gymData?.Routes == null || gymData?.Ascends == null)
            return null;

        var walls = gymData.GymDetails.Walls.ToDictionary(w => w.Id);
        var holds = gymData.GymDetails.Holds.ToDictionary(h => h.Id);

        var ascendsByClimbs = gymData.Ascends
            .Where(a => a.TopType > RouteTopType.NotTopped && a.DateLogged >= DateTime.Today.AddDays(-daysBack))
            .GroupBy(a => a.ClimbId);

        var bestAscends = new List<Route>();
        foreach (var ascendsByClimb in ascendsByClimbs)
        {
            var route = gymData.Routes.SingleOrDefault(r => r.Id == ascendsByClimb.Key);
            if (route?.Grade.GetGradeNumber() == null || !walls.ContainsKey(route.WallId)) continue;

            var bestAttempt = ascendsByClimb
                .OrderByDescending(a => a.GetGradeWithBonus(route.Grade.GetGradeNumber()!.Value))
                .ThenByDescending(a => a.DateLogged)
                .First();

            var bestAscend = new Route
            {
                Id = route.Id,
                Grade = route.Grade.GetGradeNumber().GetFrenchGrade(),
                GradeNumber = route.Grade.GetGradeNumber(),
                Rope = route.RopeNumber == 0 ? "/" : route.RopeNumber.ToString(),
                Wall = walls[route.WallId].Name,
                Color = holds[route.HoldId].GetRouteColor(),
                Live = route.Live,
                Deleted = route.Deleted,
                BestAttemptScore = bestAttempt.GetGradeWithBonus(route.Grade.GetGradeNumber()!.Value),
                BestAttemptDateLogged = bestAttempt.DateLogged
            };

            var expires = bestAscend.BestAttemptDateLogged.HasValue ? daysBack - (DateTime.Today - bestAscend.BestAttemptDateLogged!).Value.TotalDays : 0;
            bestAscend.Top10String = bestAscend.BestAttemptScore.HasValue && bestAscend.BestAttemptScore != bestAscend.GradeNumber
                ? $"{bestAscend.Grade} - Rope {bestAscend.Rope} {bestAscend.Wall} - Score: {bestAscend.BestAttemptScore} (+{bestAscend.BestAttemptScore-bestAscend.GradeNumber ?? 0}) - Exp: {expires:0}d"
                : $"{bestAscend.Grade} - Rope {bestAscend.Rope} {bestAscend.Wall} - Score: {bestAscend.BestAttemptScore ?? 0} - Exp: {expires:0}d";

            bestAscends.Add(bestAscend);
        }
        return bestAscends;
    }
    private async Task<GymData?> GetGymData(bool refresh)
    {
        GymData? gymData;
        if (refresh || !File.Exists(_gymData))
        {
            if (!File.Exists(_gymNameFile) || !File.Exists(_userIdFile))
                return null;

            var gymName = File.ReadAllText(_gymNameFile);
            if (!long.TryParse(File.ReadAllText(_userIdFile), out var userUId))
                return null;

            var gymDetails = await _topLoggerService.GetGymByName(gymName);
            if (gymDetails == null)
                return null;

            var routes = await _topLoggerService.GetRoutes(gymDetails.Id);
            var ascends = await _topLoggerService.GetAscends(userUId, gymDetails.Id);

            gymData = new GymData { UserUId = userUId, GymDetails = gymDetails, Routes = routes, Ascends = ascends };
            File.WriteAllText(_gymData, JsonSerializer.Serialize(gymData));
        }
        else
        {
            gymData = JsonSerializer.Deserialize<GymData?>(File.ReadAllText(_gymData));
        }
        return gymData;
    }
    public Route? GetRouteById(int routeId)
    {
        if (!File.Exists(_processedRoutes)) return null;

        var routes = JsonSerializer.Deserialize<List<Route>>(File.ReadAllText(_processedRoutes));
        return routes?.FirstOrDefault(r => r.Id == routeId);
    }

    public async Task<RouteCommunityInfo?> GetRouteCommunityInfo(int routeId)
    {
        if (!File.Exists(_gymIdFile) || !int.TryParse(File.ReadAllText(_gymIdFile), out var gymId))
            return null;

        var routeStats = await _topLoggerService.GetRouteStats(gymId, routeId);
        if (routeStats == null) return null;

        var grades = routeStats.CommunityGrades?.OrderBy(g => g.Grade)
            .Select(g => $"{g.Grade.GetGradeNumber().GetFrenchGrade()} ({g.Count} votes)")
            .ToList() ?? new List<string>();

        var stars = routeStats.CommunityOpinions?
            .Where(o => o.Votes > 0).OrderBy(o => o.Stars)
            .Select(o => $"{o.Stars} stars ({o.Votes} votes)")
            .ToList() ?? new List<string>();

        var toppers = new List<string>();
        foreach (var topper in routeStats.Toppers)
        {
            var topOpinion = (await _topLoggerService.GetOpinions(topper.UId, gymId, routeId)).FirstOrDefault();
            toppers.Add(topOpinion?.Grade == null ? topper.ToString() : $"{topper} (voted {topOpinion.Grade.GetGradeNumber().GetFrenchGrade()})");
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
        if (File.Exists(_gymData)) File.Delete(_gymData);
    }
}
