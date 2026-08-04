namespace Operations.Api.Contracts;

public sealed record DashboardResponse(FleetMetricsResponse FleetMetrics, IReadOnlyList<FarmDashboardSummaryResponse> Farms);
public sealed record FleetMetricsResponse(double AveragePowerKw, double AverageWindMs, int CriticalAlertCount);
public sealed record FarmDashboardSummaryResponse(string FarmId, string FarmName, double AveragePowerKw, double AverageWindMs, int CriticalAlertCount);
public sealed record FarmResponse(string FarmId, string FarmName, IReadOnlyList<FarmTurbineResponse> Turbines);
public sealed record FarmTurbineResponse(string TurbineId);
public sealed record TurbineResponse(string TurbineId, string? TurbineName, string FarmId, string FarmName, double? AveragePowerKw, double? AverageWindMs, int? CriticalAlertCount, TelemetrySeriesResponse PowerOutput, TelemetrySeriesResponse WindSpeed);
public sealed record TelemetrySeriesResponse(string Unit, IReadOnlyList<TelemetryPointResponse> Points);
public sealed record TelemetryPointResponse(DateTimeOffset Timestamp, double Value);
public sealed record AlertResponse(string Category, string Severity, string? FarmId, string? FarmName, string? TurbineId, string? TurbineName, DateTimeOffset Timestamp, string Title, string Explanation);
