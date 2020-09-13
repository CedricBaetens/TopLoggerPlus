using RestSharp;
using System.Collections.Generic;
using TopLoggerPlus.ApiWrapper.Model;

namespace TopLoggerPlus.ApiWrapper
{
    public class TopLoggerService
    {
        private RestClient _client;

        public TopLoggerService()
        {
            _client = new RestClient("https://api.toplogger.nu/v1");
        }

        public List<Route> GetRoutes()
        {
            var request = new RestRequest("gyms/49/climbs.json", DataFormat.Json);
            request.AddQueryParameter("json_params", "{\"filters\":{\"deleted\":false,\"live\":true}}");

            var response = _client.Get<List<Route>>(request);
            return response.Data;
        }
        public List<Ascend> GetAscends()
        {
            var request = new RestRequest("ascends.json", DataFormat.Json);
            request.AddQueryParameter("json_params", "{\"filters\":{\"used\":true,\"user\":{\"uid\":\"9889643268\"},\"climb\":{\"gym_id\":49,\"deleted\":false,\"liv\":true}}}");
            request.AddQueryParameter("serialize_checks", "true");

            var response = _client.Get<List<Ascend>>(request);
            return response.Data;
        }
        public Gym GetGym()
        {
            var request = new RestRequest("gyms/klimax.json", DataFormat.Json);
            request.AddQueryParameter("json_params", "{\"includes\":[\"holds\",\"walls\",\"setters\"]}");

            var response = _client.Get<Gym>(request);

            return response.Data;
        }
    }
}
