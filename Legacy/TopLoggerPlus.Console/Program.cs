using System;
using TopLoggerPlus.ApiWrapper;
using TopLoggerPlus.Logic;

namespace TopLoggerPlus.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            var api = new TopLoggerService();


            var test = api.GetAscends();
        }
    }
}
