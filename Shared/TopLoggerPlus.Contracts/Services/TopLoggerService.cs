namespace TopLoggerPlus.Contracts.Services;

public interface ITopLoggerService
{
    Task<string?> GetGyms();
    Task<string?> GetGymByName(string name);
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

    public async Task<string?> GetGyms()
    {
        try
        {
            var response = await _topLoggerApi.GetGyms();
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
    public async Task<string?> GetGymByName(string name)
    {
        try
        {
            var response = await _topLoggerApi.GetGymByName(name);
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
}
