using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using OperationsApi.Application;
using OperationsApi.Infrastructure;
using Xunit;

namespace OperationsApi.Tests;

public sealed class OperationsApiTests(OperationsApiFactory factory) : IClassFixture<OperationsApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public void Csv_loader_reads_all_valid_seed_rows()
    {
        var environment = new TestHostEnvironment(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../OperationsApi")));
        var store = new OperationsStore(environment, Microsoft.Extensions.Logging.Abstractions.NullLogger<OperationsStore>.Instance);
        Assert.Equal(10, store.Farms.Count);
        Assert.Equal(2, store.Turbines.Count);
        Assert.Equal(1122, store.Telemetry.Count);
    }

    [Fact]
    public async Task Dashboard_has_seed_aggregates_and_only_reporting_farms()
    {
        var response = await _client.GetFromJsonAsync<DashboardResponse>("/api/operations/dashboard");
        Assert.NotNull(response);
        Assert.Equal(2, response.Farms.Count);
        Assert.Equal(2737.569340463459, response.FleetMetrics.AveragePowerKw, 6);
        Assert.Equal(10.914260249554, response.FleetMetrics.AverageWindMs, 6);
        Assert.Equal(6, response.FleetMetrics.CriticalAlertCount);
    }

    [Fact]
    public async Task Farm_returns_mapped_turbines_and_farms_without_telemetry()
    {
        var farm = await _client.GetFromJsonAsync<FarmResponse>("/api/operations/farms/FARM01");
        Assert.NotNull(farm);
        Assert.Equal("FARM01", farm.FarmId);
        Assert.Equal(["TURB001"], farm.Turbines.Select(t => t.TurbineId));
        var emptyFarm = await _client.GetFromJsonAsync<FarmResponse>("/api/operations/farms/FARM03");
        Assert.NotNull(emptyFarm);
        Assert.Empty(emptyFarm.Turbines);
    }

    [Theory]
    [InlineData("/api/operations/farms/NOPE", "farm_not_found")]
    [InlineData("/api/operations/turbines/NOPE", "turbine_not_found")]
    public async Task Missing_resources_return_contract_404_shape(string path, string code)
    {
        var response = await _client.GetAsync(path);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal(code, error.Code);
        Assert.False(string.IsNullOrWhiteSpace(error.Message));
    }

    [Fact]
    public async Task Turbine_series_are_typed_and_chronological()
    {
        var turbine = await _client.GetFromJsonAsync<TurbineResponse>("/api/operations/turbines/TURB001");
        Assert.NotNull(turbine);
        Assert.Equal("kW", turbine.PowerOutput.Unit);
        Assert.Equal("m/s", turbine.WindSpeed.Unit);
        Assert.Equal(562, turbine.PowerOutput.Points.Count);
        Assert.True(turbine.PowerOutput.Points.Zip(turbine.PowerOutput.Points.Skip(1)).All(pair => pair.First.Timestamp <= pair.Second.Timestamp));
    }

    [Fact]
    public async Task Alerts_cover_every_seed_category_with_expected_counts()
    {
        var alerts = await _client.GetFromJsonAsync<List<AlertResponse>>("/api/operations/alerts");
        Assert.NotNull(alerts);
        Assert.Equal(3, alerts.Count(a => a.Category == "zero_power_high_wind"));
        Assert.Equal(3, alerts.Count(a => a.Category == "high_gearbox_temperature"));
        Assert.Equal(30, alerts.Count(a => a.Category == "missing_interval"));
        Assert.Equal(312, alerts.Count(a => a.Category == "late_arrival"));
        Assert.True(alerts.Zip(alerts.Skip(1)).All(pair => pair.First.Timestamp <= pair.Second.Timestamp));
    }

    [Fact]
    public async Task Compatibility_alias_and_documentation_are_available()
    {
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/api/dashboard")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/openapi/v1.yaml")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/swagger/index.html")).StatusCode);
    }
}

file sealed class TestHostEnvironment(string contentRootPath) : Microsoft.Extensions.Hosting.IHostEnvironment
{
    public string EnvironmentName { get; set; } = "Test";
    public string ApplicationName { get; set; } = "OperationsApi.Tests";
    public string ContentRootPath { get; set; } = contentRootPath;
    public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(contentRootPath);
}
