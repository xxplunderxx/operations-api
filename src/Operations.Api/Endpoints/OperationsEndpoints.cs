namespace Operations.Api.Endpoints;

public static class OperationsEndpoints
{
    public static IEndpointRouteBuilder MapOperationsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var operations = endpoints.MapGroup("/api/operations");
        operations.MapDashboardEndpoints();
        operations.MapFarmEndpoints();
        operations.MapTurbineEndpoints();
        operations.MapAlertEndpoints();
        return endpoints;
    }
}
