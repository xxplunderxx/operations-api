using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Operations.Api.Infrastructure;
using Operations.Api.Options;
using Xunit;

namespace Operations.Tests;

public sealed class CsvOperationsRepositoryTests
{
    [Fact]
    public void Missing_required_data_file_fails_with_a_clear_error()
    {
        var directory = Directory.CreateTempSubdirectory("operations-api-");
        try
        {
            File.WriteAllText(Path.Combine(directory.FullName, "farms.csv"), "id,name,latitude,longitude\n");

            var exception = Assert.Throws<InvalidOperationException>(() => CreateRepository(directory.FullName));

            Assert.Contains("missing required files", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("turbines.csv", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public void Malformed_and_orphaned_rows_are_skipped()
    {
        var directory = Directory.CreateTempSubdirectory("operations-api-");
        try
        {
            File.WriteAllText(Path.Combine(directory.FullName, "farms.csv"), "id,name,latitude,longitude\nFARM01,Farm,0,0\n");
            File.WriteAllText(Path.Combine(directory.FullName, "turbines.csv"), "id,farmId,farmName,latitude,longitude\nTURB001,FARM01,Farm,0,0\n");
            File.WriteAllText(Path.Combine(directory.FullName, "telemetry.csv"), "turbineId,farmId,timestamp,receivedAt,powerOutputKw,windSpeedMs,rotorRpm,bladePitchDeg,gearboxTempC\nTURB001,FARM01,2026-01-01T00:00:00Z,2026-01-01T00:00:00Z,1,2,3,4,5\nTURB001,FARM01,bad,2026-01-01T00:00:00Z,1,2,3,4,5\nUNKNOWN,FARM01,2026-01-01T00:00:00Z,2026-01-01T00:00:00Z,1,2,3,4,5\n");

            var data = CreateRepository(directory.FullName).GetData();

            Assert.Single(data.Farms);
            Assert.Single(data.Turbines);
            Assert.Single(data.Telemetry);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    private static CsvOperationsRepository CreateRepository(string directory) => new(
        new TestHostEnvironment(directory),
        Options.Create(new CsvDataOptions { DataDirectory = "." }),
        NullLogger<CsvOperationsRepository>.Instance);

    private sealed class TestHostEnvironment(string contentRootPath) : Microsoft.Extensions.Hosting.IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Test";
        public string ApplicationName { get; set; } = "Operations.Tests";
        public string ContentRootPath { get; set; } = contentRootPath;
        public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider(contentRootPath);
    }
}
