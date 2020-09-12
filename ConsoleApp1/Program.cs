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

            var routes = service.GetRouteOverview();

            foreach (var route in routes.OrderBy(x=>x.GradeNumber).ThenBy(x=>x.Rope))
            {
                Console.WriteLine($"{route.Grade} {route.Wall} touw {route.Rope} {route.Color}: {route.Climbed}");
            }
        }
    }
}
