using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Operations.Api.Contracts;
using Xunit;

namespace Operations.Tests;

public sealed class OperationsApiTests(OperationsApiFactory factory) : IClassFixture<OperationsApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Dashboard_has_aggregates_for_all_reporting_farms()
    {
        var dashboard = await _client.GetFromJsonAsync<DashboardResponse>("/api/operations/dashboard");

        Assert.NotNull(dashboard);
        Assert.Equal(10, dashboard.Farms.Count);
        Assert.Equal(2555.883452338451, dashboard.FleetMetrics.AveragePowerKw, 6);
        Assert.Equal(9.216744019993, dashboard.FleetMetrics.AverageWindMs, 6);
        Assert.Equal(6, dashboard.FleetMetrics.CriticalAlertCount);
    }

    [Fact]
    public async Task Farm_and_turbine_routes_return_contracts()
    {
        var farm = await _client.GetFromJsonAsync<FarmResponse>("/api/operations/farms/FARM01");
        var turbine = await _client.GetFromJsonAsync<TurbineResponse>("/api/operations/turbines/TURB001");

        Assert.NotNull(farm);
        Assert.Equal(["TURB001"], farm.Turbines.Select(value => value.TurbineId));
        Assert.NotNull(turbine);
        Assert.Equal("kW", turbine.PowerOutput.Unit);
        Assert.Equal(562, turbine.PowerOutput.Points.Count);
        Assert.True(turbine.PowerOutput.Points.Zip(turbine.PowerOutput.Points.Skip(1)).All(pair => pair.First.Timestamp <= pair.Second.Timestamp));
    }

    [Fact]
    public async Task Alerts_have_expected_counts_and_ordering()
    {
        var alerts = await _client.GetFromJsonAsync<List<AlertResponse>>("/api/operations/alerts");

        Assert.NotNull(alerts);
        Assert.Equal(3, alerts.Count(alert => alert.Category == "zero_power_high_wind"));
        Assert.Equal(3, alerts.Count(alert => alert.Category == "high_gearbox_temperature"));
        Assert.Equal(158, alerts.Count(alert => alert.Category == "missing_interval"));
        Assert.Equal(396, alerts.Count(alert => alert.Category == "late_arrival"));
        Assert.True(alerts.Zip(alerts.Skip(1)).All(pair => pair.First.Timestamp <= pair.Second.Timestamp));
    }

    [Theory]
    [InlineData("/api/operations/farms/NOPE", "farm_not_found")]
    [InlineData("/api/operations/turbines/NOPE", "turbine_not_found")]
    public async Task Missing_resources_return_problem_details(string path, string code)
    {
        var response = await _client.GetAsync(path);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Resource not found", document.RootElement.GetProperty("title").GetString());
        Assert.Equal(code, document.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Health_documentation_cors_and_canonical_routing_are_available()
    {
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/health")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/openapi/v1.yaml")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/swagger/index.html")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync("/api/dashboard")).StatusCode);

        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/operations/dashboard");
        request.Headers.Add("Origin", "http://localhost:5173");
        request.Headers.Add("Access-Control-Request-Method", "GET");
        var corsResponse = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NoContent, corsResponse.StatusCode);
        Assert.Equal("http://localhost:5173", corsResponse.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }
}
