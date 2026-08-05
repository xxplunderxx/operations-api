using Operations.Api.Contracts;
using Operations.Domain;

namespace Operations.Api.Endpoints;

public static class FarmEndpoints
{
    public static RouteGroupBuilder MapFarmEndpoints(this RouteGroupBuilder operations)
    {
        operations.MapGet("/farms/{farmId}", GetFarm)
            .WithName("GetFarm")
            .WithTags("Farms");
        return operations;
    }

    private static IResult GetFarm(string farmId, OperationsQueries queries) => queries.GetFarm(farmId) is { } farm
        ? Results.Ok(farm.ToResponse())
        : EndpointProblem.NotFound("farm_not_found", $"Farm '{farmId}' was not found.");
}
