using RestSharp;
using System.Collections.Generic;
using System.Threading.Tasks;
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

        public async Task<List<Route>> GetRoutes(int gymId)
        {
            var request = new RestRequest($"gyms/{gymId}/climbs.json", DataFormat.Json);
            request.AddQueryParameter("json_params", "{\"filters\":{\"deleted\":false,\"live\":true}}");

            return await _client.GetAsync<List<Route>>(request);
        }
        public async Task<List<Ascend>> GetAscends(string userId, int gymId)
        {
            var request = new RestRequest("ascends.json", DataFormat.Json);
            request.AddQueryParameter("json_params", $"{{\"filters\":{{\"used\":true,\"user\":{{\"uid\":\"{userId}\"}},\"climb\":{{\"gym_id\":{gymId},\"deleted\":false,\"liv\":true}}}}}}");
            request.AddQueryParameter("serialize_checks", "true");
            return await _client.GetAsync<List<Ascend>>(request);
        }

        public async Task<Gym> GetGym(string name)
        {
            var request = new RestRequest($"gyms/{name}.json", DataFormat.Json);
            request.AddQueryParameter("json_params", "{\"includes\":[\"holds\",\"walls\",\"setters\"]}");
            return await _client.GetAsync<Gym>(request);
        }
        public async Task<List<Gym>> GetGyms()
        {
            var request = new RestRequest("gyms.json", DataFormat.Json);
            request.AddQueryParameter("json_params", "{\"includes\":[\"gym_resources\"]}");
            return await _client.GetAsync<List<Gym>>(request);
        }
    }
}
