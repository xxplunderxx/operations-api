namespace Operations.Domain;

public sealed record Farm(string Id, string Name, double Latitude, double Longitude);

public sealed record Turbine(string Id, string FarmId, string FarmName, double Latitude, double Longitude);

public sealed record Telemetry(
    string TurbineId,
    string FarmId,
    DateTimeOffset Timestamp,
    DateTimeOffset ReceivedAt,
    double PowerOutputKw,
    double WindSpeedMs,
    double RotorRpm,
    double BladePitchDeg,
    double GearboxTempC);
