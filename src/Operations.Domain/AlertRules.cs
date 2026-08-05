namespace Operations.Domain;

public sealed class AlertRules
{
    public AlertRules(AlertSettings settings) => Settings = settings;

    public AlertSettings Settings { get; }

    public IEnumerable<OperationalAlert> Operational(Telemetry row)
    {
        if (row.PowerOutputKw == 0 && row.WindSpeedMs >= Settings.HighWindMinimumMs)
        {
            yield return new OperationalAlert(
                "zero_power_high_wind",
                "Zero power in high wind",
                $"Power was 0 kW while wind speed was {row.WindSpeedMs:0.##} m/s (threshold {Settings.HighWindMinimumMs:0.##} m/s).");
        }

        if (row.GearboxTempC > Settings.GearboxCriticalTemperatureC)
        {
            yield return new OperationalAlert(
                "high_gearbox_temperature",
                "High gearbox temperature",
                $"Gearbox temperature was {row.GearboxTempC:0.##} °C (threshold {Settings.GearboxCriticalTemperatureC:0.##} °C).");
        }
    }
}

public sealed record AlertSettings(
    double HighWindMinimumMs,
    double GearboxCriticalTemperatureC,
    TimeSpan ExpectedCadence,
    TimeSpan LateArrivalThreshold);

public sealed record OperationalAlert(string Category, string Title, string Explanation);
