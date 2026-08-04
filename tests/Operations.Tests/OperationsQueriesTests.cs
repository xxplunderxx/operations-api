using Operations.Domain;
using Xunit;

namespace Operations.Tests;

public sealed class OperationsQueriesTests
{
    private static readonly AlertSettings AlertSettings = new(
        HighWindMinimumMs: 10,
        GearboxCriticalTemperatureC: 100,
        ExpectedCadence: TimeSpan.FromMinutes(5),
        LateArrivalThreshold: TimeSpan.FromMinutes(10));

    [Fact]
    public void Alert_boundaries_do_not_trigger_until_the_threshold_is_exceeded()
    {
        var timestamp = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var telemetry = new[]
        {
            Row(timestamp, timestamp + AlertSettings.LateArrivalThreshold, 0, AlertSettings.HighWindMinimumMs - 1, AlertSettings.GearboxCriticalTemperatureC),
            Row(timestamp + AlertSettings.ExpectedCadence, timestamp + AlertSettings.ExpectedCadence, 1, 9, 99),
        };
        var queries = CreateQueries(telemetry);

        Assert.Empty(queries.GetAlerts());
    }

    [Fact]
    public void Alerts_cover_operational_late_arrival_and_missing_interval_rules()
    {
        var timestamp = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var telemetry = new[]
        {
            Row(timestamp, timestamp + TimeSpan.FromMinutes(11), 0, 10, 101),
            Row(timestamp + TimeSpan.FromMinutes(6), timestamp + TimeSpan.FromMinutes(6), 1, 8, 90),
        };
        var queries = CreateQueries(telemetry);

        var alerts = queries.GetAlerts();

        Assert.Equal(4, alerts.Count);
        Assert.Contains(alerts, alert => alert.Category == "zero_power_high_wind" && alert.Severity == AlertSeverity.Critical);
        Assert.Contains(alerts, alert => alert.Category == "high_gearbox_temperature" && alert.Severity == AlertSeverity.Critical);
        Assert.Contains(alerts, alert => alert.Category == "late_arrival" && alert.Severity == AlertSeverity.Warning);
        Assert.Contains(alerts, alert => alert.Category == "missing_interval" && alert.Severity == AlertSeverity.Warning);
    }

    [Fact]
    public void Farm_without_telemetry_is_returned_but_not_in_dashboard()
    {
        var queries = new OperationsQueries(new InMemoryRepository(new OperationsData(
            [new Farm("FARM01", "Reporting", 0, 0), new Farm("FARM02", "Empty", 0, 0)],
            [new Turbine("TURB001", "FARM01", "Reporting", 0, 0), new Turbine("TURB002", "FARM02", "Empty", 0, 0)],
            [Row(DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, 2, 5, 50)])), new AlertRules(AlertSettings));

        var dashboard = queries.GetDashboard();
        var farm = queries.GetFarm("FARM02");

        Assert.Single(dashboard.Farms);
        Assert.Equal("FARM01", dashboard.Farms[0].FarmId);
        Assert.NotNull(farm);
        Assert.Single(farm.Turbines);
    }

    [Fact]
    public void Missing_resources_return_null()
    {
        var queries = CreateQueries([]);

        Assert.Null(queries.GetFarm("missing"));
        Assert.Null(queries.GetTurbine("missing"));
    }

    private static InMemoryRepository CreateRepository(IReadOnlyList<Telemetry> telemetry) => new(new OperationsData(
        [new Farm("FARM01", "Farm", 0, 0)],
        [new Turbine("TURB001", "FARM01", "Farm", 0, 0)],
        telemetry));

    private static OperationsQueries CreateQueries(IReadOnlyList<Telemetry> telemetry) =>
        new(CreateRepository(telemetry), new AlertRules(AlertSettings));

    private static Telemetry Row(DateTimeOffset timestamp, DateTimeOffset receivedAt, double power, double wind, double gearboxTemperature) =>
        new("TURB001", "FARM01", timestamp, receivedAt, power, wind, 0, 0, gearboxTemperature);

    private sealed class InMemoryRepository(OperationsData data) : IOperationsRepository
    {
        public OperationsData GetData() => data;
    }
}
