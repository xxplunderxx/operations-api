namespace OperationsApi.Application;

public sealed record DashboardResponse(FleetMetrics FleetMetrics, IReadOnlyList<FarmDashboardSummary> Farms);
public sealed record FleetMetrics(double AveragePowerKw, double AverageWindMs, int CriticalAlertCount);
public sealed record FarmDashboardSummary(string FarmId, string FarmName, double AveragePowerKw, double AverageWindMs, int CriticalAlertCount);
public sealed record FarmResponse(string FarmId, string FarmName, IReadOnlyList<FarmTurbine> Turbines);
public sealed record FarmTurbine(string TurbineId);
public sealed record TurbineResponse(string TurbineId, string? TurbineName, string FarmId, string FarmName,
    double? AveragePowerKw, double? AverageWindMs, int? CriticalAlertCount, TelemetrySeries PowerOutput, TelemetrySeries WindSpeed);
public sealed record TelemetrySeries(string Unit, IReadOnlyList<TelemetryPoint> Points);
public sealed record TelemetryPoint(DateTimeOffset Timestamp, double Value);
public sealed record AlertResponse(string Category, string Severity, string? FarmId, string? FarmName, string? TurbineId,
    string? TurbineName, DateTimeOffset Timestamp, string Title, string Explanation);
public sealed record ErrorResponse(string Code, string Message);
