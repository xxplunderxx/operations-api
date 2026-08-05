using System.Globalization;
using Microsoft.Extensions.Options;
using Operations.Api.Contracts;
using Operations.Api.Options;
using Operations.Domain;

namespace Operations.Api.Endpoints;

public static class TurbineEndpoints
{
    public static RouteGroupBuilder MapTurbineEndpoints(this RouteGroupBuilder operations)
    {
        operations.MapGet("/turbines/{turbineId}", GetTurbine)
            .WithName("GetTurbine")
            .WithTags("Turbines");
        return operations;
    }

    private static IResult GetTurbine(string turbineId, HttpRequest request, OperationsQueries queries, IOptions<PaginationOptions> pagination)
    {
        if (!TryCreateRequest(request, pagination.Value, out var query, out var problem))
        {
            return problem!;
        }

        var result = queries.GetTurbineTelemetry(turbineId, query!.Metric, query.Start, query.PageSize);
        if (result is null)
        {
            return EndpointProblem.NotFound("turbine_not_found", $"Turbine '{turbineId}' was not found.");
        }

        var self = request.Path + request.QueryString;
        var next = result.HasMore
            ? $"{request.Path}?metric={query.MetricValue}&page%5Bsize%5D={query.PageSize}&page%5Bafter%5D={EncodeCursor(result.NextStart)}"
            : null;
        return Results.Json(result.ToResponse(self, next, query.PageSize), contentType: "application/vnd.api+json");
    }

    private static bool TryCreateRequest(HttpRequest request, PaginationOptions pagination, out TurbineTelemetryQuery? query, out IResult? problem)
    {
        var metricValue = request.Query["metric"].ToString();
        if (!Enum.TryParse<TurbineMetric>(metricValue, ignoreCase: false, out var metric) || !Enum.IsDefined(metric))
        {
            query = null;
            problem = EndpointProblem.BadRequest("invalid_metric", "Query parameter 'metric' is required and must be one of: powerOutput, windSpeed, gearBoxTemp.");
            return false;
        }

        var pageSize = ParsePageSize(request.Query["page[size]"].ToString(), pagination.DefaultPageSize, pagination.MaxPageSize);
        if (pageSize is null)
        {
            query = null;
            problem = EndpointProblem.BadRequest("invalid_page_size", $"Query parameter 'page[size]' must be an integer between 1 and {pagination.MaxPageSize}.");
            return false;
        }

        if (!TryDecodeCursor(request.Query["page[after]"].ToString(), out var start))
        {
            query = null;
            problem = EndpointProblem.BadRequest("invalid_page_cursor", "Query parameter 'page[after]' must be a valid cursor returned by this endpoint.");
            return false;
        }

        query = new TurbineTelemetryQuery(metric, metricValue, start, pageSize.Value);
        problem = null;
        return true;
    }

    private static int? ParsePageSize(string value, int defaultSize, int max) => string.IsNullOrEmpty(value)
        ? defaultSize
        : int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var size) && size > 0
            ? Math.Min(size, max)
            : null;

    private static bool TryDecodeCursor(string value, out int start)
    {
        if (string.IsNullOrEmpty(value))
        {
            start = 0;
            return true;
        }

        try
        {
            var padded = value.Replace('-', '+').Replace('_', '/') + new string('=', (4 - value.Length % 4) % 4);
            return int.TryParse(System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(padded)), NumberStyles.None, CultureInfo.InvariantCulture, out start) && start >= 0;
        }
        catch (FormatException)
        {
            start = 0;
            return false;
        }
    }

    private static string EncodeCursor(int start) => Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(start.ToString(CultureInfo.InvariantCulture)))
        .TrimEnd('=')
        .Replace('+', '-')
        .Replace('/', '_');

    private sealed record TurbineTelemetryQuery(TurbineMetric Metric, string MetricValue, int Start, int PageSize);
}
