using OperationsApi.Application;
using OperationsApi.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<OperationsStore>();
builder.Services.AddSingleton<OperationsService>();
builder.Services.AddCors(options => options.AddPolicy("local-ui", policy =>
    policy.WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.MapGet("/openapi/v1.yaml", () => Results.File(Path.Combine(AppContext.BaseDirectory, "openapi", "operations-api.yaml"), "application/yaml"));
app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.yaml", "Operations API v1"));
app.UseCors("local-ui");
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

var canonical = app.MapGroup("/api/operations");
var compatibility = app.MapGroup("/api");
MapOperations(canonical);
MapOperations(compatibility);

app.Run();

static void MapOperations(RouteGroupBuilder routes)
{
    routes.MapGet("/dashboard", (OperationsService service) => Results.Ok(service.GetDashboard()));
    routes.MapGet("/farms/{farmId}", (string farmId, OperationsService service) =>
        service.GetFarm(farmId) is { } farm ? Results.Ok(farm) : Results.NotFound(new ErrorResponse("farm_not_found", $"Farm '{farmId}' was not found.")));
    routes.MapGet("/turbines/{turbineId}", (string turbineId, OperationsService service) =>
        service.GetTurbine(turbineId) is { } turbine ? Results.Ok(turbine) : Results.NotFound(new ErrorResponse("turbine_not_found", $"Turbine '{turbineId}' was not found.")));
    routes.MapGet("/alerts", (OperationsService service) => Results.Ok(service.GetAlerts()));
}

public partial class Program { }
