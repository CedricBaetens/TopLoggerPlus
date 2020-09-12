using System;
using System.Collections.Generic;
using System.Linq;
using TopLoggerPlus.Logic;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            var service = new TopLoggerApiService();
            
            // Get Data
            var gym = service.GetGym();
            var routes = service.GetRoutes();

            // Get Sectors
            var walls = gym.walls.Where(x => x.name.Contains("Sector"));
            var holds = gym.holds.ToDictionary(x=>x.id);

            var routesFilterd = routes
                .Where(x => walls.Select(x => x.id).Contains(x.wall_id))
                .ToList();

            foreach (var routeByGrade in routesFilterd.OrderBy(x=>x.grade).GroupBy(x=>x.grade))
            {
                Console.WriteLine($"{service.GradeConvertor(routeByGrade.Key)}: {routeByGrade.Count()}");

                foreach (var route in routeByGrade.OrderBy(x=>x.rope_number).ToList())
                {
                    Console.WriteLine($" -  touw {route.rope_number} {holds[route.hold_id].brand}");
                }
            }
        }
    }
}
