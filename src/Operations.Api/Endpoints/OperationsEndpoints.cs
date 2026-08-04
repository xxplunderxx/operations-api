using System.Globalization;
using Microsoft.Extensions.Options;
using Operations.Api.Contracts;
using Operations.Api.Options;
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

    private static IResult GetTurbine(string turbineId, HttpRequest request, OperationsQueries queries, IOptions<PaginationOptions> pagination)
    {
        var metricValue = request.Query["metric"].ToString();
        if (!Enum.TryParse<TurbineMetric>(metricValue, ignoreCase: false, out var metric) || !Enum.IsDefined(metric))
        {
            return EndpointProblem.BadRequest("invalid_metric", "Query parameter 'metric' is required and must be one of: powerOutput, windSpeed, gearBoxTemp.");
        }

        var pageSize = ParsePageSize(request.Query["page[size]"].ToString(), pagination.Value.DefaultPageSize, pagination.Value.MaxPageSize);
        if (pageSize is null)
        {
            return EndpointProblem.BadRequest("invalid_page_size", $"Query parameter 'page[size]' must be an integer between 1 and {pagination.Value.MaxPageSize}.");
        }

        var cursorValue = request.Query["page[after]"].ToString();
        if (!TryDecodeCursor(cursorValue, out var start))
        {
            return EndpointProblem.BadRequest("invalid_page_cursor", "Query parameter 'page[after]' must be a valid cursor returned by this endpoint.");
        }

        var result = queries.GetTurbineTelemetry(turbineId, metric, start, pageSize.Value);
        if (result is null)
        {
            return EndpointProblem.NotFound("turbine_not_found", $"Turbine '{turbineId}' was not found.");
        }

        var self = request.Path + request.QueryString;
        var next = result.HasMore ? $"{request.Path}?metric={metricValue}&page%5Bsize%5D={pageSize.Value}&page%5Bafter%5D={EncodeCursor(result.NextStart)}" : null;
        return Results.Json(result.ToResponse(self, next, pageSize.Value), contentType: "application/vnd.api+json");
    }

    private static int? ParsePageSize(string value, int defaultSize, int max) => string.IsNullOrEmpty(value) ? defaultSize : int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var size) && size > 0 ? Math.Min(size, max) : null;

    private static bool TryDecodeCursor(string value, out int start)
    {
        if (string.IsNullOrEmpty(value)) { start = 0; return true; }
        try { return int.TryParse(System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(value.Replace('-', '+').Replace('_', '/') + new string('=', (4 - value.Length % 4) % 4))), NumberStyles.None, CultureInfo.InvariantCulture, out start) && start >= 0; }
        catch (FormatException) { start = 0; return false; }
    }

    private static string EncodeCursor(int start) => Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(start.ToString(CultureInfo.InvariantCulture))).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
