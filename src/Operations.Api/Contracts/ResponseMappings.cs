using Operations.Domain;

namespace Operations.Api.Contracts;

public static class ResponseMappings
{
    public static DashboardResponse ToResponse(this Dashboard value) => new(
        new FleetMetricsResponse(value.FleetMetrics.AveragePowerKw, value.FleetMetrics.AverageWindMs, value.FleetMetrics.AverageGearboxTempC, value.FleetMetrics.CriticalAlertCount),
        value.Farms.Select(farm => new FarmDashboardSummaryResponse(farm.FarmId, farm.FarmName, farm.AveragePowerKw, farm.AverageWindMs, farm.AverageGearboxTempC, farm.CriticalAlertCount)).ToArray());

    public static FarmResponse ToResponse(this FarmDetails value) => new(value.FarmId, value.FarmName, value.Turbines.Select(turbine => new FarmTurbineResponse(turbine.TurbineId)).ToArray());

    public static TurbineResponse ToResponse(this TurbineDetails value) => new(value.TurbineId, null, value.FarmId, value.FarmName, value.AveragePowerKw, value.AverageWindMs, value.AverageGearboxTempC, value.CriticalAlertCount, value.PowerOutput.ToResponse(), value.WindSpeed.ToResponse());

    public static AlertResponse ToResponse(this Alert value) => new(value.Category, value.Severity.ToString().ToLowerInvariant(), value.FarmId, value.FarmName, value.TurbineId, null, value.Timestamp, value.Title, value.Explanation);

    public static JsonApiDocumentResponse ToResponse(this TurbineTelemetryPage value, string self, string? next, int pageSize) => new(
        value.Points.Select((point, index) => new JsonApiResourceResponse("telemetry", $"{value.TurbineId}:{value.NextStart - value.Points.Count + index}:{point.Timestamp:O}", new TelemetryPointResponse(point.Timestamp, point.Value))).ToArray(),
        new JsonApiLinks(self, next),
        new JsonApiPageMeta(value.Metric.ToString(), value.Metric.Unit(), pageSize, value.HasMore, new TurbinePageMeta(value.TurbineId, value.FarmId, value.FarmName, value.AveragePowerKw, value.AverageWindMs, value.AverageGearboxTempC, value.CriticalAlertCount)));

    private static TelemetrySeriesResponse ToResponse(this TelemetrySeries value) => new(value.Unit, value.Points.Select(point => new TelemetryPointResponse(point.Timestamp, point.Value)).ToArray());
}
