namespace TopLoggerPlus.Contracts.Services.TopLoggerApi;

public interface ITopLoggerApi
{
    [Get("/v1/gyms.json")]
    Task<string> GetGyms([AliasAs("json_params")] string jsonParams = "{\"includes\":[\"gym_resources\"]}");

    [Get("/v1/gyms/{name}.json")]
    Task<string> GetGymByName(string name, [AliasAs("json_params")] string jsonParams = "{\"includes\":[\"holds\",\"setters\",\"walls\"]}");
}
