using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TopLoggerPlus.ApiWrapper;
using TopLoggerPlus.Logic.Model;

namespace TopLoggerPlus.Logic
{
    public class RouteService
    {
        private readonly TopLoggerService _topLoggerService;

        public RouteService(TopLoggerService topLoggerService)
        {
            _topLoggerService = topLoggerService;
        }

        public async Task<List<Route>> GetRoutesAsync(string userId)
        {
            var result = new List<Route>();

            // Create Tasks
            var gymTask = _topLoggerService.GetGym("klimax");
            var routesTask = _topLoggerService.GetRoutes(49);
            var ascendsTask = _topLoggerService.GetAscends(userId, 49);

            // Gym data
            var gym = await gymTask;
            var walls = gym.walls.ToDictionary(x => x.id);
            var holds = gym.holds.ToDictionary(x => x.id);

            // Routes
            var routesFilterd = (await routesTask)
                .Where(x => walls.ContainsKey(x.wall_id))
                .ToList();

            // Ascends
            var ascends = await ascendsTask;

            foreach (var apiRoute in routesFilterd)
            {
                var route = new Route
                {
                    Grade = GradeConvertor(apiRoute.grade),
                    GradeNumber = apiRoute.grade,
                    Rope = apiRoute.rope_number == 0 ? "/" : apiRoute.rope_number.ToString(),
                    Wall = walls[apiRoute.wall_id].name,
                };

                // Ascend
                var routeAscend = ascends.FirstOrDefault(x => x.climb_id == apiRoute.id);
                if (routeAscend == null)
                {
                    route.TopType = RouteTopType.NotTopped;
                }
                else
                {
                    switch (routeAscend.checks)
                    {
                        case 1:
                            route.TopType = RouteTopType.RedPoint;
                            break;
                        case 2:
                            route.TopType = RouteTopType.Flash;
                            break;
                        case 3:
                            route.TopType = RouteTopType.OnSight;
                            break;
                    }
                }

                // Color
                var hold = holds[apiRoute.hold_id];
                route.Color = new RouteColor()
                {
                    Name = hold.brand,
                    Value = hold.color
                };
                result.Add(route);
            }
            return result;
        }

        public async Task<List<Route>> GetRoutesAsync(string userId, string userId2)
        {
            var result = new List<Route>();

            // Create Tasks
            var gymTask = _topLoggerService.GetGym("klimax");
            var routesTask = _topLoggerService.GetRoutes(49);
            var ascends1Task = _topLoggerService.GetAscends(userId, 49);
            var ascends2Task = _topLoggerService.GetAscends(userId2, 49);

            // Gym data
            var gym = await gymTask;
            var walls = gym.walls.ToDictionary(x => x.id);
            var holds = gym.holds.ToDictionary(x => x.id);

            // Routes
            var routesFilterd = (await routesTask)
                .Where(x => walls.ContainsKey(x.wall_id))
                .ToList();

            // Ascends
            var ascendsUser1 = await ascends1Task;
            var ascendsUser2 = await ascends2Task;

            foreach (var apiRoute in routesFilterd)
            {
                if (ascendsUser1.Any(x => x.climb_id == apiRoute.id) || ascendsUser2.Any(x => x.climb_id == apiRoute.id))
                    continue;

                var route = new Route
                {
                    Grade = GradeConvertor(apiRoute.grade),
                    GradeNumber = apiRoute.grade,
                    Rope = apiRoute.rope_number == 0 ? "/" : apiRoute.rope_number.ToString(),
                    Wall = walls[apiRoute.wall_id].name,
                    TopType = RouteTopType.NotTopped
                };


                // Color
                var hold = holds[apiRoute.hold_id];
                route.Color = new RouteColor()
                {
                    Name = hold.brand,
                    Value = hold.color
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
}
