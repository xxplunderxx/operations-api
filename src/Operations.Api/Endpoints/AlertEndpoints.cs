using Operations.Api.Contracts;
using Operations.Domain;

namespace Operations.Api.Endpoints;

public static class AlertEndpoints
{
    public static RouteGroupBuilder MapAlertEndpoints(this RouteGroupBuilder operations)
    {
        operations.MapGet("/alerts", GetAlerts)
            .WithName("GetAlerts")
            .WithTags("Alerts");
        return operations;
    }

    private static IResult GetAlerts(OperationsQueries queries) =>
        Results.Ok(queries.GetAlerts().Select(alert => alert.ToResponse()).ToArray());
}
