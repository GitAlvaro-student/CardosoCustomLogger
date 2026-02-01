// See https://aka.ms/new-console-template for more information
using CustomLogger.Buffering;
using CustomLogger.Configurations;
using CustomLogger.Providers;
using CustomLogger.Sinks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Xunit;

// Constrói a configuração
var builder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appSettings.json", optional: false, reloadOnChange: true);

IConfiguration config = builder.Build();

// Ler valores simples
string container = config["Azure:ContainerName"]!;
string connectionString = config["Azure:ConnectionString"]!;

#region OriginalLogs
var options = new CustomProviderOptions
{
    MinimumLogLevel = LogLevel.Trace,
    UseGlobalBuffer = true,
    BatchOptions = new BatchOptions
    {
        BatchSize = 5,
        FlushInterval = TimeSpan.FromSeconds(5)
    }
};

var provider = new CustomLoggerProviderBuilder()
    .WithOptions(options)
    .AddConsoleSink()
    .AddFileSink("logs/app.log")
    //.AddBlobSink(connectionString, container)
    .Build();

var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddProvider(provider);
    builder.SetMinimumLevel(options.MinimumLogLevel);
});

var logger = loggerFactory.CreateLogger("Test");


var activity = new Activity("CommerceConsoleAppActivity").Start();
var scopes = new Dictionary<string, object>
{
    ["traceId"] = activity.Context.TraceId.ToString(),
    ["spanId"] = activity.Context.SpanId.ToString()
};

using (logger.BeginScope(scopes))
{
    logger.LogTrace("This is a trace log, useful for debugging.");
    logger.LogDebug("This is a debug log, useful for development.");
    logger.LogInformation("This is an information log, useful for general information.");
    logger.LogWarning("This is a warning log, indicating a potential issue.");
    logger.LogError(new InvalidOperationException("Invalid Generic Operation"), "This is an error log, indicating a failure in the application.");
    logger.LogCritical("This is a critical log, indicating a severe failure.");
}

provider.Dispose();

activity.Stop();
Thread.Sleep(1000);

#endregion
