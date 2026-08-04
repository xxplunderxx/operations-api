using System.Globalization;
using Microsoft.Extensions.Options;
using Operations.Api.Options;
using Operations.Domain;

namespace Operations.Api.Infrastructure;

public sealed partial class CsvOperationsRepository : IOperationsRepository
{
    private static readonly string[] RequiredFiles = ["farms.csv", "turbines.csv", "telemetry.csv"];
    private readonly OperationsData _data;

    public CsvOperationsRepository(IHostEnvironment environment, IOptions<CsvDataOptions> options, ILogger<CsvOperationsRepository> logger)
    {
        var directory = Path.GetFullPath(Path.Combine(environment.ContentRootPath, options.Value.DataDirectory));
        EnsureRequiredFiles(directory);

        var farms = Read(directory, "farms.csv", ParseFarm, logger);
        var turbines = Read(directory, "turbines.csv", ParseTurbine, logger);
        var knownTurbines = turbines.Select(turbine => turbine.Id).ToHashSet(StringComparer.Ordinal);
        var knownFarms = farms.Select(farm => farm.Id).ToHashSet(StringComparer.Ordinal);
        var telemetry = Read(directory, "telemetry.csv", ParseTelemetry, logger)
            .Where(row => knownTurbines.Contains(row.TurbineId) && knownFarms.Contains(row.FarmId))
            .OrderBy(row => row.Timestamp)
            .ThenBy(row => row.TurbineId, StringComparer.Ordinal)
            .ToArray();

        _data = new OperationsData(farms, turbines, telemetry);
        LogLoaded(logger, farms.Count, turbines.Count, telemetry.Length, directory);
    }

    public OperationsData GetData() => _data;

    private static void EnsureRequiredFiles(string directory)
    {
        var missingFiles = RequiredFiles
            .Where(fileName => !File.Exists(Path.Combine(directory, fileName)))
            .ToArray();
        if (missingFiles.Length > 0)
        {
            throw new InvalidOperationException($"CSV data directory '{directory}' is missing required files: {string.Join(", ", missingFiles)}.");
        }
    }

    private static List<T> Read<T>(string directory, string fileName, Func<string[], T> parse, ILogger logger)
    {
        var validRows = new List<T>();
        var lineNumber = 1;
        foreach (var line in File.ReadLines(Path.Combine(directory, fileName)).Skip(1))
        {
            lineNumber++;
            try
            {
                validRows.Add(parse(line.Split(',', StringSplitOptions.None)));
            }
            catch (Exception exception) when (exception is FormatException or IndexOutOfRangeException or ArgumentException)
            {
                LogMalformedRow(logger, fileName, lineNumber, exception.Message);
            }
        }

        return validRows;
    }

    private static Farm ParseFarm(string[] values) => new(values[0], values[1], Number(values[2]), Number(values[3]));

    private static Turbine ParseTurbine(string[] values) => new(values[0], values[1], values[2], Number(values[3]), Number(values[4]));

    private static Telemetry ParseTelemetry(string[] values) => new(
        values[0],
        values[1],
        DateTimeOffset.Parse(values[2], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal),
        DateTimeOffset.Parse(values[3], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal),
        Number(values[4]),
        Number(values[5]),
        Number(values[6]),
        Number(values[7]),
        Number(values[8]));

    private static double Number(string value) => double.Parse(value, CultureInfo.InvariantCulture);

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Loaded {FarmCount} farms, {TurbineCount} turbines, and {TelemetryCount} valid telemetry rows from {DataDirectory}")]
    private static partial void LogLoaded(ILogger logger, int farmCount, int turbineCount, int telemetryCount, string dataDirectory);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Skipping malformed {FileName} row {LineNumber}: {Message}")]
    private static partial void LogMalformedRow(ILogger logger, string fileName, int lineNumber, string message);
}
