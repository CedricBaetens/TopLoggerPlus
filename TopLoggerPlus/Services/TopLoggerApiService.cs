using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TopLoggerPlus.Services
{
    public class TopLoggerApiService
    {
        private RestClient _client;

        public TopLoggerApiService()
        {
            _client = new RestClient("https://api.toplogger.nu/v1");
        }

        public Task GetRoutes()
        {
            var request = new RestRequest("gyms/49/climbs.json", DataFormat.Json);

            request.AddParameter("json_params", new Root()
            {
                filters = new Filters() { deleted = false, live = true }
            });
            var response = _client.Get(request);

            var test = response.StatusCode;


            return Task.CompletedTask;
        }


        public class Filters
        {
            public bool deleted { get; set; }
            public bool live { get; set; }
        }
        public class Root
        {
            public Filters filters { get; set; }
        }
    }
}
