using Operations.Api.Contracts;
using Operations.Domain;

namespace Operations.Api.Endpoints;

public static class DashboardEndpoints
{
    public static RouteGroupBuilder MapDashboardEndpoints(this RouteGroupBuilder operations)
    {
        operations.MapGet("/dashboard", GetDashboard)
            .WithName("GetDashboard")
            .WithTags("Dashboard");
        return operations;
    }

    private static IResult GetDashboard(OperationsQueries queries) => Results.Ok(queries.GetDashboard().ToResponse());
}
