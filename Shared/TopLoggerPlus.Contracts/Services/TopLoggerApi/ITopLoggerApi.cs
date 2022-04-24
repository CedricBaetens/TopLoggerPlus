namespace TopLoggerPlus.Contracts.Services.TopLoggerApi;

public interface ITopLoggerApi
{
    [Get("/v1/gyms.json")]
    Task<List<Gym>> GetGyms([AliasAs("json_params")] string jsonParams);

    [Get("/v1/gyms/{name}.json")]
    Task<Gym?> GetGymByName(string name, [AliasAs("json_params")] string jsonParams);
    [Get("/v1/gyms/{gymId}/climbs.json")]
    Task<List<Route>> GetRoutes(int gymId, [AliasAs("json_params")] string jsonParams);
    [Get("/v1/gyms/{gymId}/ranked_users.json")]
    Task<List<User>> GetUsers(int gymId, [AliasAs("climbs_type")] string climbTypes, [AliasAs("ranking_type")] string rankingType);

    [Get("/v1/ascends.json")]
    Task<List<Ascend>> GetAscends([AliasAs("json_params")] string jsonParams, [AliasAs("serialize_checks")] string serializeChecks);
}
