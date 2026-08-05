using System.ComponentModel.DataAnnotations;
using Operations.Domain;

namespace Operations.Api.Options;

public sealed class AlertRuleOptions
{
    public const string SectionName = "AlertRules";

    [Range(0d, double.MaxValue)]
    public double HighWindMinimumMs { get; init; }

    [Range(0d, double.MaxValue)]
    public double GearboxCriticalTemperatureC { get; init; }

    [Range(1, int.MaxValue)]
    public int ExpectedCadenceMinutes { get; init; }

    [Range(1, int.MaxValue)]
    public int LateArrivalThresholdMinutes { get; init; }

    public AlertSettings ToSettings() => new(
        HighWindMinimumMs,
        GearboxCriticalTemperatureC,
        TimeSpan.FromMinutes(ExpectedCadenceMinutes),
        TimeSpan.FromMinutes(LateArrivalThresholdMinutes));
}
