namespace Operations.Domain;

public interface IOperationsRepository
{
    OperationsData GetData();
}

public sealed record OperationsData(
    IReadOnlyList<Farm> Farms,
    IReadOnlyList<Turbine> Turbines,
    IReadOnlyList<Telemetry> Telemetry);
