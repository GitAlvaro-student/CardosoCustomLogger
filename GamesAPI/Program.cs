using CustomLogger.Abstractions;
using CustomLogger.AspNetCore.HealthChecks;
using CustomLogger.HealthChecks;
using CustomLogger.HealthChecks.Abstractions;
using CustomLogger.OpenTelemetry;
using CustomLogger.Providers;
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
    .AddCustomLogger();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IJogoService, JogoService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health");

app.Run();
