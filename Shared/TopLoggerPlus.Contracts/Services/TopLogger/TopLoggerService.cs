namespace TopLoggerPlus.Contracts.Services.TopLogger;

public interface ITopLoggerService
{
    Task<List<Gym>?> GetGyms();

    Task<GymDetails?> GetGymByName(string name);
    Task<List<Route>> GetRoutes(int gymId);
    Task<RouteStats?> GetRouteStats(int gymId, int routeId);
    Task<List<User>> GetUsers(int gymId);

    Task<List<Ascend>> GetAscends(long userUId, int gymId);
    Task<List<Opinion>> GetOpinions(long userUId, int gymId, int? routeId = null);
}

public class TopLoggerService : ITopLoggerService
{
    private readonly ILogger<TopLoggerService> _logger;
    private readonly ITopLoggerApi _topLoggerApi;

    public TopLoggerService(ILogger<TopLoggerService> logger, ITopLoggerApi topLoggerApi)
    {
        _logger = logger;
        _topLoggerApi = topLoggerApi;
    }

    public async Task<List<Gym>?> GetGyms()
    {
        try
        {
            var jsonParams = "{\"includes\":[\"gym_resources\"]}";
            var response = await _topLoggerApi.GetGyms(jsonParams);
            _logger.LogDebug("TopLogger Response {0}", response);
            return response;
        }
        catch (ApiException apiEx)
        {
            _logger.LogError(apiEx, $"{nameof(GetGyms)} Error: {apiEx.StatusCode}, {apiEx.ReasonPhrase}");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{nameof(GetGyms)} Exception");
            return null;
        }
    }

    public async Task<GymDetails?> GetGymByName(string name)
    {
        try
        {
            var jsonParams = "{\"includes\":[\"holds\",\"setters\",\"walls\"]}";
            var response = await _topLoggerApi.GetGymByName(name, jsonParams);
            _logger.LogDebug("TopLogger Response {0}", response);
            return response;
        }
        catch (ApiException apiEx)
        {
            _logger.LogError(apiEx, $"{nameof(GetGyms)} Error: {apiEx.StatusCode}, {apiEx.ReasonPhrase}");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{nameof(GetGyms)} Exception");
            return null;
        }
    }
    public async Task<List<Route>> GetRoutes(int gymId)
    {
        try
        {
            var jsonParams = "{\"filters\":{\"deleted\":false,\"live\":true}}";
            var response = await _topLoggerApi.GetRoutes(gymId, jsonParams);
            _logger.LogDebug("TopLogger Response {0}", response);
            return response;
        }
        catch (ApiException apiEx)
        {
            _logger.LogError(apiEx, $"{nameof(GetRoutes)} Error: {apiEx.StatusCode}, {apiEx.ReasonPhrase}");
            return new List<Route>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{nameof(GetRoutes)} Exception");
            return new List<Route>();
        }
    }
    public async Task<RouteStats?> GetRouteStats(int gymId, int routeId)
    {
        try
        {
            var response = await _topLoggerApi.GetRouteStats(gymId, routeId);
            _logger.LogDebug("TopLogger Response {0}", response);
            return response;
        }
        catch (ApiException apiEx)
        {
            _logger.LogError(apiEx, $"{nameof(GetRouteStats)} Error: {apiEx.StatusCode}, {apiEx.ReasonPhrase}");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{nameof(GetRouteStats)} Exception");
            return null;
        }
    }
    public async Task<List<User>> GetUsers(int gymId)
    {
        try
        {
            var response = await _topLoggerApi.GetUsers(gymId, "routes", "grade");
            _logger.LogDebug("TopLogger Response {0}", response);
            return response;
        }
        catch (ApiException apiEx)
        {
            _logger.LogError(apiEx, $"{nameof(GetUsers)} Error: {apiEx.StatusCode}, {apiEx.ReasonPhrase}");
            return new List<User>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{nameof(GetUsers)} Exception");
            return new List<User>();
        }
    }

    public async Task<List<Ascend>> GetAscends(long userUId, int gymId)
    {
        try
        {
            var jsonParams = $"{{\"filters\":{{\"used\":true,\"user\":{{\"uid\":\"{userUId}\"}},\"climb\":{{\"gym_id\":{gymId},\"live\":true}}}}}}";
            var response = await _topLoggerApi.GetAscends(jsonParams, "true");
            _logger.LogDebug("TopLogger Response {0}", response);
            return response;
        }
        catch (ApiException apiEx)
        {
            _logger.LogError(apiEx, $"{nameof(GetAscends)} Error: {apiEx.StatusCode}, {apiEx.ReasonPhrase}");
            return new List<Ascend>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{nameof(GetAscends)} Exception");
            return new List<Ascend>();
        }
    }
    public async Task<List<Opinion>> GetOpinions(long userUId, int gymId, int? routeId = null)
    {
        try
        {
            var jsonParams = routeId.HasValue
                ? $"{{\"filters\":{{\"used\":true,\"user\":{{\"uid\":\"{userUId}\"}},\"climb\":{{\"gym_id\":{gymId},\"id\":{routeId.Value},\"live\":true}}}}}}"
                : $"{{\"filters\":{{\"used\":true,\"user\":{{\"uid\":\"{userUId}\"}},\"climb\":{{\"gym_id\":{gymId},\"live\":true}}}}}}";
            var response = await _topLoggerApi.GetOpinions(jsonParams);
            _logger.LogDebug("TopLogger Response {0}", response);
            return response;
        }
        catch (ApiException apiEx)
        {
            _logger.LogError(apiEx, $"{nameof(GetOpinions)} Error: {apiEx.StatusCode}, {apiEx.ReasonPhrase}");
            return new List<Opinion>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{nameof(GetOpinions)} Exception");
            return new List<Opinion>();
        }
    }
}
