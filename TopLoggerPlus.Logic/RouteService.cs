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

        public async Task<List<Route>> GetRoutes()
        {
            var result = new List<Route>();

            // Get Data
            var gym = _topLoggerService.GetGym();
            var routes = _topLoggerService.GetRoutes();
            var ascends = _topLoggerService.GetAscends();

            // Get Sectors
            var walls = gym.walls.Where(x => x.name.ToUpper().Contains("SECTOR")).ToDictionary(x => x.id);
            var holds = gym.holds.ToDictionary(x => x.id);

            var routesFilterd = routes
                .Where(x => walls.ContainsKey(x.wall_id))
                .ToList();

            foreach (var route in routesFilterd)
            {
                var routeOverview = new Route
                {
                    Grade = GradeConvertor(route.grade),
                    GradeNumber = route.grade,
                    Rope = route.rope_number == 0 ? "/" : route.rope_number.ToString(),
                    Wall = walls[route.wall_id].name,
                };

                // Ascend
                var routeAscend = ascends.FirstOrDefault(x => x.climb_id == route.id);
                if(routeAscend == null)
                {
                    routeOverview.TopType = RouteTopType.NotTopped;
                }
                else
                {
                    switch (routeAscend.checks)
                    {
                        case 1:
                            routeOverview.TopType = RouteTopType.RedPoint;
                            break;
                        case 2:
                            routeOverview.TopType = RouteTopType.Flash;
                            break;
                        case 3:
                            routeOverview.TopType = RouteTopType.OnSight;
                            break;
                    }
                }

                // Color
                var hold = holds[route.hold_id];
                routeOverview.Color = new RouteColor()
                {
                    Name = hold.brand,
                    Value = hold.color
                };

                result.Add(routeOverview);

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
