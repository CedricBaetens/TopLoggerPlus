using TopLoggerPlus.Contracts.Services.GraphQL;
using Gym = TopLoggerPlus.Contracts.Domain.Gym;
using User = TopLoggerPlus.Contracts.Domain.User;
using Route = TopLoggerPlus.Contracts.Domain.Route;

namespace TopLoggerPlus.Contracts.Services;

public interface IToploggerService
{
    Task Login(string refreshToken);
    void Logout();
    void ClearAll();
    
    Task<User> GetMyUserInfo();
    
    Task<(List<Route> routes, DateTime syncTime)> GetRoutes(bool refresh = false);
    Task<List<Route>> GetBestAscends(int daysBack, bool refresh = false);
    Route GetRouteById(string routeId);
    
    Task<RouteCommunityInfo> GetRouteCommunityInfo(string routeId);
}

public class ToploggerService(
    IGraphQLService graphQLService,
    IAuthenticationService authenticationService,
    IStorageService storageService,
    HttpClient httpClient)
    : IToploggerService
{
    public async Task Login(string refreshToken)
    {
        await authenticationService.RefreshAccessToken(refreshToken);
    }
    public void Logout()
    {
        authenticationService.Logout();
    }
    public void ClearAll()
    {
        storageService.ResetStorage();
    }
    
    public async Task<User> GetMyUserInfo()
    {
        var userInfo = await graphQLService.GetMyUserInfo();
        storageService.Write("UserInfo", userInfo);
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
        var (routes, ropeNumbers) = await GetGymData(refresh);
        if (routes == null)
            return (null, DateTime.Now);
        
        var processedRoutes = routes.Select(apiRoute => new Route
            {
                Id = apiRoute.Id,
                Grade = apiRoute.Grade.GetFrenchGrade(),
                GradeNumber = apiRoute.Grade,
                Rope = apiRoute.GetRopeNumber(ropeNumbers),
                Wall = apiRoute.Wall?.NameLoc ?? "unknown",
                Color = apiRoute.HoldColor.GetRouteColor(),
                Setter = apiRoute.ClimbSetters?.FirstOrDefault()?.GymAdmin?.Name,
                AscendsInfo = apiRoute.ClimbUser == null
                    ? null
                    : new AscendsInfo
                    {
                        MyGrade = apiRoute.ClimbUser.Grade?.GetFrenchGrade(),
                        MyGradeNumber = apiRoute.ClimbUser.Grade,
                        Score = apiRoute.ClimbUser.GetGradeWithBonus(apiRoute.Grade),
                        TotalTries = apiRoute.ClimbUser.TotalTries,
                        TopType = apiRoute.ClimbUser.TickType,
                        TriedFirstAt = apiRoute.ClimbUser.TriedFirstAtDate,
                        ToppedFirstAt = apiRoute.ClimbUser.TickedFirstAtDate
                    },
                InAt = apiRoute.InAt,
                OutPlannedAt = apiRoute.OutPlannedAt,
                OutAt = apiRoute.OutAt
            })
            .ToList();

        storageService.Write("ProcessedRoutes", processedRoutes);
        return (processedRoutes.Where(r => !r.OutAt.HasValue || r.OutAt.Value < DateTime.Now).ToList(), DateTime.Now);
    }
    public async Task<List<Route>> GetBestAscends(int daysBack, bool refresh = false)
    {
        var (routes, ropeNumbers) = await GetGymData(refresh);
        if (routes == null)
            return null;

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
    private async Task<(List<Climb>, List<RopeNumber>)> GetGymData(bool refresh)
    {
        List<Climb> routes = [];
        List<RopeNumber> ropeNumbers;
        
        if (!refresh)
        {
            routes = storageService.Read<List<GraphQL.Climb>>("RawRoutes");
            ropeNumbers = storageService.Read<List<RopeNumber>>("RopeNumbers");
            if (routes != null) return (routes, ropeNumbers);
        }
        
        var userInfo = storageService.Read<GraphQL.User>("UserInfo");
        if (string.IsNullOrEmpty(userInfo?.Id) || string.IsNullOrEmpty(userInfo.Gym?.Id))
            throw new AuthenticationFailedException("No user found");
        
        routes = await graphQLService.GetClimbs(userInfo.Gym.Id, userInfo.Id);
        storageService.Write("RawRoutes", routes);

        try
        {
            var response = await httpClient.GetAsync("https://github.com/CedricBaetens/TopLoggerPlus/raw/refs/heads/master/Content/klimax.csv");
            var content = await response.Content.ReadAsStringAsync();
            ropeNumbers = content.Split('\n')
                .Select(record => record.Split(';'))
                .Where(fields => fields.Length == 4)
                .Select(fields => new RopeNumber
                {
                    Wall = fields[0],
                    Color = fields[1],
                    Grade = int.TryParse(fields[2], out var grade) ? grade : 0,
                    Number = int.TryParse(fields[3], out var number) ? number : 0,
                })
                .ToList();
            storageService.Write("RopeNumbers", ropeNumbers);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            ropeNumbers = null;
        }

        return (routes, ropeNumbers);
    }
    
    public Route GetRouteById(string routeId)
    {
        var processedRoutes = storageService.Read<List<Route>>("ProcessedRoutes");
        return processedRoutes?.FirstOrDefault(r => r.Id == routeId);
    }
    
    public Task<RouteCommunityInfo> GetRouteCommunityInfo(string routeId)
    {
        return Task.FromResult<RouteCommunityInfo>(null);
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
}
