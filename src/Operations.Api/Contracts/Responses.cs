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
public sealed record JsonApiDocumentResponse(IReadOnlyList<JsonApiResourceResponse> Data, JsonApiLinks Links, JsonApiPageMeta Meta);
public sealed record JsonApiResourceResponse(string Type, string Id, TelemetryPointResponse Attributes);
public sealed record JsonApiLinks(string Self, string? Next);
public sealed record JsonApiPageMeta(string Metric, string Unit, int Size, bool HasMore, TurbinePageMeta Turbine);
public sealed record TurbinePageMeta(string TurbineId, string FarmId, string FarmName, double? AveragePowerKw, double? AverageWindMs, int? CriticalAlertCount);
