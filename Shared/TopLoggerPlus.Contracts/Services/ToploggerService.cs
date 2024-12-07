using TopLoggerPlus.Contracts.Services.GraphQL;
using Gym = TopLoggerPlus.Contracts.Domain.Gym;
using User = TopLoggerPlus.Contracts.Domain.User;
using Route = TopLoggerPlus.Contracts.Domain.Route;
using Ascend = TopLoggerPlus.Contracts.Domain.Ascend;

namespace TopLoggerPlus.Contracts.Services;

public interface IToploggerService
{
    Task Login(string refreshToken);
    void Logout();
    
    Task<User> GetMyUserInfo();
    
    Task<(List<Route> routes, DateTime syncTime)> GetRoutes(bool refresh = false);
    Task<List<Route>> GetBestAscends(int daysBack, bool refresh = false);
    Route GetRouteById(string routeId);
    
    Task<RouteCommunityInfo> GetRouteCommunityInfo(string routeId);

    void ClearAll();
}

public class ToploggerService : IToploggerService
{
    private readonly IGraphQLService _graphQLService;
    private readonly IAuthenticationService _authenticationService;
    private readonly IStorageService _storageService;

    public ToploggerService(IGraphQLService graphQLService, IAuthenticationService authenticationService, IStorageService storageService)
    {
        _graphQLService = graphQLService;
        _authenticationService = authenticationService;
        _storageService = storageService;
    }

    public async Task Login(string refreshToken)
    {
        await _authenticationService.RefreshAccessToken(refreshToken);
    }
    public void Logout()
    {
        _authenticationService.Logout();
    }
    
    public async Task<User> GetMyUserInfo()
    {
        var userInfo = await _graphQLService.GetMyUserInfo();
        _storageService.Write("UserInfo", userInfo);
        return new User
        {
            Id = userInfo.Id,
            Name = userInfo.FullName,
            Gym = new Gym { Id = userInfo.Gym.Id, Name = userInfo.Gym.NameSlug },
            FavoriteGyms = userInfo.GymUserFavorites?
                .Select(g => new Gym { Id = g.Gym.Id, Name = g.Gym.Name })
                .ToList() ?? new List<Gym>()
        };
    }

    public async Task<(List<Route> routes, DateTime syncTime)> GetRoutes(bool refresh = false)
    {
        var gymData = await GetGymData(refresh);
        if (gymData?.Routes == null)
            return (null, DateTime.Now);
        
        var routes = gymData.Routes
            .ToList();
        
        var processedRoutes = new List<Route>();
        foreach (var apiRoute in routes)
        {
            var route = new Route
            {
                Id = apiRoute.Id,
                Grade = apiRoute.Grade.GetFrenchGrade(),
                GradeNumber = apiRoute.Grade,
                Rope = "/",
                Wall = apiRoute.Wall?.NameLoc ?? "unknown",
                Color = apiRoute.HoldColor.GetRouteColor(),
                Live = apiRoute.InAt.HasValue && apiRoute.InAt.Value < DateTime.Now,
                Deleted = apiRoute.OutAt.HasValue && apiRoute.OutAt.Value < DateTime.Now
            };
        
            processedRoutes.Add(route);
        }
        
        _storageService.Write("ProcessedRoutes", processedRoutes);
        return (processedRoutes.Where(r => !r.Deleted).ToList(), DateTime.Now);
        
        // var gymData = await GetGymData(refresh);
        // if (gymData?.UserUId == null || gymData?.GymDetails == null || gymData?.Routes == null || gymData?.Ascends == null)
        //     return (null, DateTime.Now);
        //
        // var walls = gymData.GymDetails.Walls.ToDictionary(w => w.Id);
        // var holds = gymData.GymDetails.Holds.ToDictionary(h => h.Id);
        //
        // var routes = gymData.Routes
        //     .Where(r => walls.ContainsKey(r.WallId))
        //     .ToList();
        //
        // //var opinionTask = _topLoggerService.GetOpinions(gymData.UserUId.Value, gymData.GymDetails.Id);
        //
        // var processedRoutes = new List<Route>();
        // foreach (var apiRoute in routes)
        // {
        //     var route = new Route
        //     {
        //         Id = apiRoute.Id,
        //         Grade = apiRoute.Grade.GetGradeNumber().GetFrenchGrade(),
        //         GradeNumber = apiRoute.Grade.GetGradeNumber(),
        //         Rope = apiRoute.RopeNumber ?? "/",
        //         Wall = walls[apiRoute.WallId].Name,
        //         Color = holds[apiRoute.HoldId].GetRouteColor(),
        //         Live = apiRoute.Live,
        //         Deleted = apiRoute.Deleted
        //     };
        //
        //     // Ascend
        //     foreach (var ascend in gymData.Ascends.Where(a => a.ClimbId == apiRoute.Id))
        //     {
        //         route.Ascends.Add(new Ascend
        //         {
        //             LoggedAt = ascend.DateLogged,
        //             TopType = ascend.TopType
        //         });
        //     }
        //
        //     // Opinion
        //     // var opinion = (await opinionTask).Where(o => o.ClimbId == apiRoute.Id).FirstOrDefault();
        //     // if (opinion != null)
        //     // {
        //     //     route.MyGrade = opinion.Grade.GetGradeNumber().GetFrenchGrade();
        //     //     route.MyGradeNumber = opinion.Grade.GetGradeNumber();
        //     // }
        //
        //     processedRoutes.Add(route);
        // }
        //
        // File.WriteAllText(_processedRoutes, JsonSerializer.Serialize(processedRoutes));
        // return (processedRoutes.Where(r => !r.Deleted).ToList(), DateTime.Now);
    }
    public async Task<List<Route>> GetBestAscends(int daysBack, bool refresh = false)
    {
        return null;
        // var gymData = await GetGymData(refresh);
        // if (gymData?.UserUId == null || gymData?.GymDetails == null || gymData?.Routes == null || gymData?.Ascends == null)
        //     return null;
        //
        // var walls = gymData.GymDetails.Walls.ToDictionary(w => w.Id);
        // var holds = gymData.GymDetails.Holds.ToDictionary(h => h.Id);
        //
        // var ascendsByClimbs = gymData.Ascends
        //     .Where(a => a.TopType > RouteTopType.NotTopped && a.DateLogged >= DateTime.Today.AddDays(-daysBack))
        //     .GroupBy(a => a.ClimbId);
        //
        // var bestAscends = new List<Route>();
        // foreach (var ascendsByClimb in ascendsByClimbs)
        // {
        //     var route = gymData.Routes.SingleOrDefault(r => r.Id == ascendsByClimb.Key);
        //     if (route?.Grade.GetGradeNumber() == null || !walls.ContainsKey(route.WallId)) continue;
        //
        //     var bestAttempt = ascendsByClimb
        //         .OrderByDescending(a => a.GetGradeWithBonus(route.Grade.GetGradeNumber()!.Value))
        //         .ThenByDescending(a => a.DateLogged)
        //         .First();
        //
        //     var bestAscend = new Route
        //     {
        //         Id = route.Id,
        //         Grade = route.Grade.GetGradeNumber().GetFrenchGrade(),
        //         GradeNumber = route.Grade.GetGradeNumber(),
        //         Rope = route.RopeNumber ?? "/",
        //         Wall = walls[route.WallId].Name,
        //         Color = holds[route.HoldId].GetRouteColor(),
        //         Live = route.Live,
        //         Deleted = route.Deleted,
        //         BestAttemptScore = bestAttempt.GetGradeWithBonus(route.Grade.GetGradeNumber()!.Value),
        //         BestAttemptDateLogged = bestAttempt.DateLogged
        //     };
        //
        //     var expires = bestAscend.BestAttemptDateLogged.HasValue ? daysBack - (DateTime.Today - bestAscend.BestAttemptDateLogged!).Value.TotalDays : 0;
        //     bestAscend.Top10String = bestAscend.BestAttemptScore.HasValue && bestAscend.BestAttemptScore != bestAscend.GradeNumber
        //         ? $"{bestAscend.Grade} - Rope {bestAscend.Rope} {bestAscend.Wall} - Score: {bestAscend.BestAttemptScore} (+{bestAscend.BestAttemptScore-bestAscend.GradeNumber ?? 0}) - Exp: {expires:0}d"
        //         : $"{bestAscend.Grade} - Rope {bestAscend.Rope} {bestAscend.Wall} - Score: {bestAscend.BestAttemptScore ?? 0} - Exp: {expires:0}d";
        //
        //     bestAscends.Add(bestAscend);
        // }
        // return bestAscends;
    }
    private async Task<GymData> GetGymData(bool refresh)
    {
        GymData gymData;
        
        if (!refresh)
        {
            gymData = _storageService.Read<GymData>("GymData");
            if (gymData != null) return gymData;
        }
        
        var userInfo = _storageService.Read<GraphQL.User>("UserInfo");
        if (string.IsNullOrEmpty(userInfo?.Id) || string.IsNullOrEmpty(userInfo.Gym?.Id))
            return null;

        var routes = await _graphQLService.GetRoutes(userInfo.Gym.Id);
        //var ascends = await _topLoggerService.GetAscends(userId, gymDetails.Id);

        gymData = new GymData
        {
            Routes = routes,
            //Ascends = ascends
        };
        _storageService.Write("GymData", gymData);

        return gymData;
    }
    public Route GetRouteById(string routeId)
    {
        var processedRoutes = _storageService.Read<List<Route>>("ProcessedRoutes");
        return processedRoutes?.FirstOrDefault(r => r.Id == routeId);
    }
    
    public async Task<RouteCommunityInfo> GetRouteCommunityInfo(string routeId)
    {
        return null;
        // if (!File.Exists(_gymIdFile) || !int.TryParse(File.ReadAllText(_gymIdFile), out var gymId))
        //     return null;
        //
        // var routeStats = await _topLoggerService.GetRouteStats(gymId, routeId);
        // if (routeStats == null) return null;
        //
        // var grades = routeStats.CommunityGrades?.OrderBy(g => g.Grade)
        //     .Select(g => $"{g.Grade.GetGradeNumber().GetFrenchGrade()} ({g.Count} votes)")
        //     .ToList() ?? new List<string>();
        //
        // var stars = routeStats.CommunityOpinions?
        //     .Where(o => o.Votes > 0).OrderBy(o => o.Stars)
        //     .Select(o => $"{o.Stars} stars ({o.Votes} votes)")
        //     .ToList() ?? new List<string>();
        //
        // var toppers = new List<string>();
        // foreach (var topper in routeStats.Toppers)
        // {
        //     var topOpinion = (await _topLoggerService.GetOpinions(topper.UId, gymId, routeId)).FirstOrDefault();
        //     toppers.Add(topOpinion?.Grade == null ? topper.ToString() : $"{topper} (voted {topOpinion.Grade.GetGradeNumber().GetFrenchGrade()})");
        // }
        //
        // return new RouteCommunityInfo
        // {
        //     CommunityGrades = grades.Any() ? string.Join(", ", grades) : "No tops",
        //     CommunityStars = stars.Any() ? string.Join(", ", stars) : "No stars",
        //     Toppers = toppers
        // };
    }

    public void ClearAll()
    {
        _storageService.ResetStorage();
    }
}
