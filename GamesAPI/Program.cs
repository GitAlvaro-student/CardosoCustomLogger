using CustomLogger.Abstractions;
using CustomLogger.AspNetCore.HealthChecks;
using CustomLogger.HealthChecks;
using CustomLogger.HealthChecks.Abstractions;
using CustomLogger.OpenTelemetry;
using CustomLogger.Providers;
using GamesAPI;
using GamesAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Logging.ClearProviders();
builder.Logging.AddCustomLogging(builder.Configuration);
builder.Services.AddCustomLoggerOpenTelemetry(builder.Configuration);

builder.Services.AddSingleton<ILoggingHealthEvaluator, DefaultLoggingHealthEvaluator>();

builder.Services.AddSingleton<ILoggingHealthState>(sp =>
{
    // Assumir que AddCustomLogging já registrou o provider
    return sp.GetService<CustomLoggerProvider>() as ILoggingHealthState
        ?? throw new InvalidOperationException("CustomLoggerProvider must implement ILoggingHealthState");
});

builder.Services.AddHealthChecks()
    // register the custom logger health check; tag as "ready" because it verifies sinks etc.
    .AddCustomLogger(name: "customlogger", failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy, tags: new[] { "ready", "logging" });

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IJogoService, JogoService>();

var app = builder.Build();

// Map three endpoints with sensible filters and a custom response writer
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("live") == true || registration.Name == null, // or only process a simple liveness check
    ResponseWriter = CustomHealthResponses.WriteMinimalResponse,
});

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready"),
    ResponseWriter = CustomHealthResponses.WriteDetailedJsonResponse,
});

app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = CustomHealthResponses.WriteDetailedJsonResponse,
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
