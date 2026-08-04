using System.Globalization;
using OperationsApi.Domain;

namespace OperationsApi.Infrastructure;

public sealed class OperationsStore
{
    public IReadOnlyList<Farm> Farms { get; }
    public IReadOnlyList<Turbine> Turbines { get; }
    public IReadOnlyList<Telemetry> Telemetry { get; }

    public OperationsStore(IHostEnvironment environment, ILogger<OperationsStore> logger)
    {
        var dataDirectory = FindDataDirectory(environment.ContentRootPath);
        Farms = Read(dataDirectory, "farms.csv", ParseFarm, logger);
        Turbines = Read(dataDirectory, "turbines.csv", ParseTurbine, logger);
        var knownTurbines = Turbines.Select(t => t.Id).ToHashSet(StringComparer.Ordinal);
        var knownFarms = Farms.Select(f => f.Id).ToHashSet(StringComparer.Ordinal);
        Telemetry = Read(dataDirectory, "telemetry.csv", ParseTelemetry, logger)
            .Where(t => knownTurbines.Contains(t.TurbineId) && knownFarms.Contains(t.FarmId))
            .OrderBy(t => t.Timestamp).ThenBy(t => t.TurbineId, StringComparer.Ordinal).ToArray();
        logger.LogInformation("Loaded {Farms} farms, {Turbines} turbines, and {Telemetry} valid telemetry rows from {DataDirectory}", Farms.Count, Turbines.Count, Telemetry.Count, dataDirectory);
    }

    private static string FindDataDirectory(string contentRoot)
    {
        var candidates = new List<string>();
        foreach (var start in new[] { contentRoot, Directory.GetCurrentDirectory() })
        {
            for (DirectoryInfo? directory = new(Path.GetFullPath(start)); directory is not null; directory = directory.Parent)
            {
                candidates.Add(Path.Combine(directory.FullName, "Data"));
                candidates.Add(Path.Combine(directory.FullName, "energy-dashboard", "Data"));
            }
        }
        var found = candidates.Distinct(StringComparer.Ordinal).FirstOrDefault(path =>
            File.Exists(Path.Combine(path, "farms.csv")) && File.Exists(Path.Combine(path, "turbines.csv")) && File.Exists(Path.Combine(path, "telemetry.csv")));
        return found ?? throw new InvalidOperationException($"Could not locate Data directory. Checked: {string.Join(", ", candidates)}");
    }

    private static IReadOnlyList<T> Read<T>(string directory, string fileName, Func<string[], T> parse, ILogger logger)
    {
        var valid = new List<T>(); var lineNumber = 1;
        foreach (var line in File.ReadLines(Path.Combine(directory, fileName)).Skip(1))
        {
            lineNumber++;
            try { valid.Add(parse(line.Split(','))); }
            catch (Exception ex) when (ex is FormatException or IndexOutOfRangeException or ArgumentException)
            { logger.LogWarning("Skipping malformed {FileName} row {LineNumber}: {Message}", fileName, lineNumber, ex.Message); }
        }
        return valid;
    }

    private static Farm ParseFarm(string[] v) => new(v[0], v[1], Number(v[2]), Number(v[3]));
    private static Turbine ParseTurbine(string[] v) => new(v[0], v[1], v[2], Number(v[3]), Number(v[4]));
    private static Telemetry ParseTelemetry(string[] v) => new(v[0], v[1], DateTimeOffset.Parse(v[2], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal), DateTimeOffset.Parse(v[3], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal), Number(v[4]), Number(v[5]), Number(v[6]), Number(v[7]), Number(v[8]));
    private static double Number(string value) => double.Parse(value, CultureInfo.InvariantCulture);
}
