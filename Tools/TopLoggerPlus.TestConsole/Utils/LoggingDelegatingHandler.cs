namespace TopLoggerPlus.TestConsole.Utils;

public class LoggingDelegatingHandler : DelegatingHandler
{
    private readonly ILogger<LoggingDelegatingHandler> _logger;

    public LoggingDelegatingHandler(ILogger<LoggingDelegatingHandler> logger)
    {
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var requestBody = request.Content != null ? await request.Content.ReadAsStringAsync() : null;
        _logger.LogDebug("RequestBody: {0}", requestBody);

        var response = await base.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync();
        _logger.LogDebug("ResponseBody: {0}", responseBody);

        return response;
    }
}
