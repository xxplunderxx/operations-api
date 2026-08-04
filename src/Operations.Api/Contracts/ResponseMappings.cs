using Operations.Domain;

namespace Operations.Api.Contracts;

public static class ResponseMappings
{
    public static DashboardResponse ToResponse(this Dashboard value) => new(
        new FleetMetricsResponse(value.FleetMetrics.AveragePowerKw, value.FleetMetrics.AverageWindMs, value.FleetMetrics.CriticalAlertCount),
        value.Farms.Select(farm => new FarmDashboardSummaryResponse(farm.FarmId, farm.FarmName, farm.AveragePowerKw, farm.AverageWindMs, farm.CriticalAlertCount)).ToArray());

    public static FarmResponse ToResponse(this FarmDetails value) => new(value.FarmId, value.FarmName, value.Turbines.Select(turbine => new FarmTurbineResponse(turbine.TurbineId)).ToArray());

    public static TurbineResponse ToResponse(this TurbineDetails value) => new(value.TurbineId, null, value.FarmId, value.FarmName, value.AveragePowerKw, value.AverageWindMs, value.CriticalAlertCount, value.PowerOutput.ToResponse(), value.WindSpeed.ToResponse());

    public static AlertResponse ToResponse(this Alert value) => new(value.Category, value.Severity.ToString().ToLowerInvariant(), value.FarmId, value.FarmName, value.TurbineId, null, value.Timestamp, value.Title, value.Explanation);

    private static TelemetrySeriesResponse ToResponse(this TelemetrySeries value) => new(value.Unit, value.Points.Select(point => new TelemetryPointResponse(point.Timestamp, point.Value)).ToArray());
}
