namespace Operations.Domain;

public static class AlertRules
{
    public const double HighWindMinimumMs = 10d;
    public const double GearboxCriticalTemperatureC = 100d;
    public static readonly TimeSpan ExpectedCadence = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan LateArrivalThreshold = TimeSpan.FromMinutes(10);

    public static IEnumerable<OperationalAlert> Operational(Telemetry row)
    {
        if (row.PowerOutputKw == 0 && row.WindSpeedMs >= HighWindMinimumMs)
        {
            yield return new OperationalAlert(
                "zero_power_high_wind",
                "Zero power in high wind",
                $"Power was 0 kW while wind speed was {row.WindSpeedMs:0.##} m/s (threshold {HighWindMinimumMs:0.##} m/s).");
        }

        if (row.GearboxTempC > GearboxCriticalTemperatureC)
        {
            yield return new OperationalAlert(
                "high_gearbox_temperature",
                "High gearbox temperature",
                $"Gearbox temperature was {row.GearboxTempC:0.##} °C (threshold {GearboxCriticalTemperatureC:0.##} °C).");
        }
    }
}

public sealed record OperationalAlert(string Category, string Title, string Explanation);
