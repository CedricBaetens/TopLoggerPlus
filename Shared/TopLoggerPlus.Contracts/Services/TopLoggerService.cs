namespace TopLoggerPlus.Contracts.Services;

public interface ITopLoggerService
{
    Task<List<Gym>?> GetGyms();

    Task<Gym?> GetGymByName(string name);
    Task<List<Route>?> GetRoutes(int gymId);
    Task<List<User>?> GetUsers(int gymId);

    Task<List<Ascend>?> GetAscends(long userUId, int gymId);
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

    public async Task<Gym?> GetGymByName(string name)
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
    public async Task<List<Route>?> GetRoutes(int gymId)
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
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{nameof(GetRoutes)} Exception");
            return null;
        }
    }
    public async Task<List<User>?> GetUsers(int gymId)
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
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{nameof(GetUsers)} Exception");
            return null;
        }
    }

    public async Task<List<Ascend>?> GetAscends(long userUId, int gymId)
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
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{nameof(GetAscends)} Exception");
            return null;
        }
    }
}
