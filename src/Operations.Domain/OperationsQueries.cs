namespace Operations.Domain;

public sealed class OperationsQueries(IOperationsRepository repository, AlertRules alertRules)
{
    public Dashboard GetDashboard()
    {
        var data = repository.GetData();
        var alerts = GetAlerts(data);
        var farms = data.Telemetry
            .GroupBy(telemetry => telemetry.FarmId)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group =>
            {
                var farm = data.Farms.Single(farm => farm.Id == group.Key);
                return new FarmDashboard(
                    farm.Id,
                    farm.Name,
                    group.Average(telemetry => telemetry.PowerOutputKw),
                    group.Average(telemetry => telemetry.WindSpeedMs),
                    CountOperationalCritical(group));
            })
            .ToArray();

        var averagePowerKw = data.Telemetry.Count == 0 ? 0 : data.Telemetry.Average(telemetry => telemetry.PowerOutputKw);
        var averageWindMs = data.Telemetry.Count == 0 ? 0 : data.Telemetry.Average(telemetry => telemetry.WindSpeedMs);

        return new Dashboard(
            new FleetMetrics(
                averagePowerKw,
                averageWindMs,
                alerts.Count(alert => alert.Severity == AlertSeverity.Critical)),
            farms);
    }

    public FarmDetails? GetFarm(string farmId)
    {
        var data = repository.GetData();
        var farm = data.Farms.FirstOrDefault(candidate => candidate.Id == farmId);
        return farm is null
            ? null
            : new FarmDetails(
                farm.Id,
                farm.Name,
                data.Turbines.Where(turbine => turbine.FarmId == farm.Id)
                    .OrderBy(turbine => turbine.Id, StringComparer.Ordinal)
                    .Select(turbine => new FarmTurbine(turbine.Id))
                    .ToArray());
    }

    public TurbineDetails? GetTurbine(string turbineId)
    {
        var data = repository.GetData();
        var turbine = data.Turbines.FirstOrDefault(candidate => candidate.Id == turbineId);
        if (turbine is null)
        {
            return null;
        }

        var rows = data.Telemetry.Where(telemetry => telemetry.TurbineId == turbine.Id)
            .OrderBy(telemetry => telemetry.Timestamp)
            .ThenBy(telemetry => telemetry.ReceivedAt)
            .ToArray();

        return new TurbineDetails(
            turbine.Id,
            turbine.FarmId,
            turbine.FarmName,
            rows.Length == 0 ? null : rows.Average(telemetry => telemetry.PowerOutputKw),
            rows.Length == 0 ? null : rows.Average(telemetry => telemetry.WindSpeedMs),
            rows.Length == 0 ? null : CountOperationalCritical(rows),
            new TelemetrySeries("kW", rows.Select(row => new TelemetryPoint(row.Timestamp, row.PowerOutputKw)).ToArray()),
            new TelemetrySeries("m/s", rows.Select(row => new TelemetryPoint(row.Timestamp, row.WindSpeedMs)).ToArray()));
    }

    public TurbineTelemetryPage? GetTurbineTelemetry(string turbineId, TurbineMetric metric, int start, int pageSize)
    {
        var data = repository.GetData();
        var turbine = data.Turbines.FirstOrDefault(candidate => candidate.Id == turbineId);
        if (turbine is null)
        {
            return null;
        }

        var rows = data.Telemetry.Where(telemetry => telemetry.TurbineId == turbine.Id)
            .OrderBy(telemetry => telemetry.Timestamp)
            .ThenBy(telemetry => telemetry.ReceivedAt)
            .ToArray();
        var page = rows.Skip(start).Take(pageSize).ToArray();
        return new TurbineTelemetryPage(
            turbine.Id,
            turbine.FarmId,
            turbine.FarmName,
            rows.Length == 0 ? null : rows.Average(row => row.PowerOutputKw),
            rows.Length == 0 ? null : rows.Average(row => row.WindSpeedMs),
            rows.Length == 0 ? null : CountOperationalCritical(rows),
            metric,
            page.Select(row => new TelemetryPoint(row.Timestamp, metric.Value(row))).ToArray(),
            start + page.Length < rows.Length,
            start + page.Length);
    }

    public IReadOnlyList<Alert> GetAlerts() => GetAlerts(repository.GetData());

    private Alert[] GetAlerts(OperationsData data)
    {
        var alerts = new List<Alert>();
        foreach (var group in data.Telemetry.GroupBy(telemetry => telemetry.TurbineId))
        {
            var turbine = data.Turbines.Single(candidate => candidate.Id == group.Key);
            var ordered = group.OrderBy(row => row.Timestamp).ThenBy(row => row.ReceivedAt).ToArray();
            foreach (var row in ordered)
            {
                foreach (var rule in alertRules.Operational(row))
                {
                    alerts.Add(CreateAlert(rule.Category, AlertSeverity.Critical, turbine, row.Timestamp, rule.Title, rule.Explanation));
                }

                if (row.ReceivedAt - row.Timestamp > alertRules.Settings.LateArrivalThreshold)
                {
                    alerts.Add(CreateAlert(
                        "late_arrival",
                        AlertSeverity.Warning,
                        turbine,
                        row.Timestamp,
                        "Late telemetry arrival",
                        $"Telemetry arrived {(row.ReceivedAt - row.Timestamp).TotalMinutes:0} minutes after measurement (threshold {alertRules.Settings.LateArrivalThreshold.TotalMinutes:0} minutes)."));
                }
            }

            for (var index = 1; index < ordered.Length; index++)
            {
                var gap = ordered[index].Timestamp - ordered[index - 1].Timestamp;
                if (gap > alertRules.Settings.ExpectedCadence)
                {
                    alerts.Add(CreateAlert(
                        "missing_interval",
                        AlertSeverity.Warning,
                        turbine,
                        ordered[index].Timestamp,
                        "Missing reporting interval",
                        $"A {gap.TotalMinutes:0}-minute gap followed the measurement at {ordered[index - 1].Timestamp:O}; expected cadence is {alertRules.Settings.ExpectedCadence.TotalMinutes:0} minutes."));
                }
            }
        }

        return alerts.OrderBy(alert => alert.Timestamp)
            .ThenBy(alert => alert.TurbineId, StringComparer.Ordinal)
            .ThenBy(alert => alert.Category, StringComparer.Ordinal)
            .ToArray();
    }

    private static Alert CreateAlert(string category, AlertSeverity severity, Turbine turbine, DateTimeOffset timestamp, string title, string explanation) =>
        new(category, severity, turbine.FarmId, turbine.FarmName, turbine.Id, timestamp, title, explanation);

    private int CountOperationalCritical(IEnumerable<Telemetry> rows) => rows.Sum(row => alertRules.Operational(row).Count());
}

public sealed record Dashboard(FleetMetrics FleetMetrics, IReadOnlyList<FarmDashboard> Farms);
public sealed record FleetMetrics(double AveragePowerKw, double AverageWindMs, int CriticalAlertCount);
public sealed record FarmDashboard(string FarmId, string FarmName, double AveragePowerKw, double AverageWindMs, int CriticalAlertCount);
public sealed record FarmDetails(string FarmId, string FarmName, IReadOnlyList<FarmTurbine> Turbines);
public sealed record FarmTurbine(string TurbineId);
public sealed record TurbineDetails(string TurbineId, string FarmId, string FarmName, double? AveragePowerKw, double? AverageWindMs, int? CriticalAlertCount, TelemetrySeries PowerOutput, TelemetrySeries WindSpeed);
public sealed record TelemetrySeries(string Unit, IReadOnlyList<TelemetryPoint> Points);
public sealed record TelemetryPoint(DateTimeOffset Timestamp, double Value);
public sealed record TurbineTelemetryPage(string TurbineId, string FarmId, string FarmName, double? AveragePowerKw, double? AverageWindMs, int? CriticalAlertCount, TurbineMetric Metric, IReadOnlyList<TelemetryPoint> Points, bool HasMore, int NextStart);
public enum TurbineMetric
{
    powerOutput,
    windSpeed,
    gearBoxTemp
}

public static class TurbineMetricExtensions
{
    public static double Value(this TurbineMetric metric, Telemetry row) => metric switch
    {
        TurbineMetric.powerOutput => row.PowerOutputKw,
        TurbineMetric.windSpeed => row.WindSpeedMs,
        TurbineMetric.gearBoxTemp => row.GearboxTempC,
        _ => throw new ArgumentOutOfRangeException(nameof(metric))
    };

    public static string Unit(this TurbineMetric metric) => metric switch
    {
        TurbineMetric.powerOutput => "kW",
        TurbineMetric.windSpeed => "m/s",
        TurbineMetric.gearBoxTemp => "°C",
        _ => throw new ArgumentOutOfRangeException(nameof(metric))
    };
}
public sealed record Alert(string Category, AlertSeverity Severity, string FarmId, string FarmName, string TurbineId, DateTimeOffset Timestamp, string Title, string Explanation);
public enum AlertSeverity { Warning, Critical }
