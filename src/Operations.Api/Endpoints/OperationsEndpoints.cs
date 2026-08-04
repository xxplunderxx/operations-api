using Operations.Api.Contracts;
using Operations.Domain;

namespace Operations.Api.Endpoints;

public static class OperationsEndpoints
{
    public static IEndpointRouteBuilder MapOperationsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var operations = endpoints.MapGroup("/api/operations").WithTags("Operations");
        operations.MapGet("/dashboard", (OperationsQueries queries) => Results.Ok(queries.GetDashboard().ToResponse()))
            .WithName("GetDashboard");
        operations.MapGet("/farms/{farmId}", GetFarm).WithName("GetFarm");
        operations.MapGet("/turbines/{turbineId}", GetTurbine).WithName("GetTurbine");
        operations.MapGet("/alerts", (OperationsQueries queries) => Results.Ok(queries.GetAlerts().Select(alert => alert.ToResponse()).ToArray()))
            .WithName("GetAlerts");
        return endpoints;
    }

    private static IResult GetFarm(string farmId, OperationsQueries queries) => queries.GetFarm(farmId) is { } farm
        ? Results.Ok(farm.ToResponse())
        : EndpointProblem.NotFound("farm_not_found", $"Farm '{farmId}' was not found.");

    private static IResult GetTurbine(string turbineId, OperationsQueries queries) => queries.GetTurbine(turbineId) is { } turbine
        ? Results.Ok(turbine.ToResponse())
        : EndpointProblem.NotFound("turbine_not_found", $"Turbine '{turbineId}' was not found.");
}
