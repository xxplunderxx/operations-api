using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.Options;
using Operations.Api.Endpoints;
using Operations.Api.Infrastructure;
using Operations.Api.Options;
using Operations.Domain;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler((ExceptionHandlerOptions _) => { });
builder.Services.AddHealthChecks();
builder.Services.AddHttpLogging(options => options.LoggingFields = HttpLoggingFields.RequestMethod | HttpLoggingFields.RequestPath | HttpLoggingFields.ResponseStatusCode);
builder.Services.AddOptions<CsvDataOptions>()
    .Bind(builder.Configuration.GetSection(CsvDataOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<AlertRuleOptions>()
    .Bind(builder.Configuration.GetSection(AlertRuleOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddSingleton<IOperationsRepository, CsvOperationsRepository>();
builder.Services.AddSingleton(sp => new AlertRules(sp.GetRequiredService<IOptions<AlertRuleOptions>>().Value.ToSettings()));
builder.Services.AddSingleton<OperationsQueries>();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy("dashboard", policy =>
{
    if (allowedOrigins.Length > 0)
    {
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
    }
}));

var app = builder.Build();

app.UseExceptionHandler();
app.UseHttpLogging();
app.UseCors("dashboard");
app.MapHealthChecks("/health");
app.MapGet("/openapi/v1.yaml", () => Results.File(Path.Combine(AppContext.BaseDirectory, "openapi", "operations-api.yaml"), "application/yaml"));
app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.yaml", "Operations API v1"));
app.MapOperationsEndpoints();

app.Run();

public partial class Program;
