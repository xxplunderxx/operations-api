using OperationsApi.Domain;
using OperationsApi.Infrastructure;

namespace OperationsApi.Application;

public sealed class OperationsService(OperationsStore store)
{
    public DashboardResponse GetDashboard()
    {
        var critical = GetAlerts().Count(a => a.Severity == "critical");
        var farms = store.Telemetry.GroupBy(t => t.FarmId).OrderBy(g => g.Key, StringComparer.Ordinal).Select(g =>
        {
            var farm = store.Farms.Single(f => f.Id == g.Key);
            return new FarmDashboardSummary(farm.Id, farm.Name, g.Average(t => t.PowerOutputKw), g.Average(t => t.WindSpeedMs), CountOperationalCritical(g));
        }).ToArray();
        return new DashboardResponse(new FleetMetrics(store.Telemetry.Average(t => t.PowerOutputKw), store.Telemetry.Average(t => t.WindSpeedMs), critical), farms);
    }

    public FarmResponse? GetFarm(string farmId) => store.Farms.FirstOrDefault(f => f.Id == farmId) is not { } farm ? null :
        new FarmResponse(farm.Id, farm.Name, store.Turbines.Where(t => t.FarmId == farm.Id).OrderBy(t => t.Id, StringComparer.Ordinal).Select(t => new FarmTurbine(t.Id)).ToArray());

    public TurbineResponse? GetTurbine(string turbineId)
    {
        var turbine = store.Turbines.FirstOrDefault(t => t.Id == turbineId);
        if (turbine is null) return null;
        var rows = store.Telemetry.Where(t => t.TurbineId == turbine.Id).OrderBy(t => t.Timestamp).ThenBy(t => t.ReceivedAt).ToArray();
        return new TurbineResponse(turbine.Id, null, turbine.FarmId, turbine.FarmName,
            rows.Length == 0 ? null : rows.Average(t => t.PowerOutputKw), rows.Length == 0 ? null : rows.Average(t => t.WindSpeedMs),
            rows.Length == 0 ? null : CountOperationalCritical(rows), new TelemetrySeries("kW", rows.Select(t => new TelemetryPoint(t.Timestamp, t.PowerOutputKw)).ToArray()), new TelemetrySeries("m/s", rows.Select(t => new TelemetryPoint(t.Timestamp, t.WindSpeedMs)).ToArray()));
    }

    public IReadOnlyList<AlertResponse> GetAlerts()
    {
        var alerts = new List<AlertResponse>();
        foreach (var group in store.Telemetry.GroupBy(t => t.TurbineId))
        {
            var turbine = store.Turbines.Single(t => t.Id == group.Key);
            var ordered = group.OrderBy(t => t.Timestamp).ThenBy(t => t.ReceivedAt).ToArray();
            foreach (var row in ordered)
            {
                foreach (var rule in AlertRules.Operational(row)) alerts.Add(Alert(rule.Category, "critical", turbine, row.Timestamp, rule.Title, rule.Explanation));
                if (row.ReceivedAt - row.Timestamp > AlertRules.LateArrivalThreshold) alerts.Add(Alert("late_arrival", "warning", turbine, row.Timestamp, "Late telemetry arrival", $"Telemetry arrived {(row.ReceivedAt - row.Timestamp).TotalMinutes:0} minutes after measurement (threshold {AlertRules.LateArrivalThreshold.TotalMinutes:0} minutes)."));
            }
            for (var i = 1; i < ordered.Length; i++)
            {
                var gap = ordered[i].Timestamp - ordered[i - 1].Timestamp;
                if (gap > AlertRules.ExpectedCadence) alerts.Add(Alert("missing_interval", "warning", turbine, ordered[i].Timestamp, "Missing reporting interval", $"A {gap.TotalMinutes:0}-minute gap followed the measurement at {ordered[i - 1].Timestamp:O}; expected cadence is {AlertRules.ExpectedCadence.TotalMinutes:0} minutes."));
            }
        }
        return alerts.OrderBy(a => a.Timestamp).ThenBy(a => a.TurbineId, StringComparer.Ordinal).ThenBy(a => a.Category, StringComparer.Ordinal).ToArray();
    }

    private static AlertResponse Alert(string category, string severity, Turbine turbine, DateTimeOffset timestamp, string title, string explanation) => new(category, severity, turbine.FarmId, turbine.FarmName, turbine.Id, null, timestamp, title, explanation);
    private static int CountOperationalCritical(IEnumerable<Telemetry> rows) => rows.Sum(row => AlertRules.Operational(row).Count());
}
