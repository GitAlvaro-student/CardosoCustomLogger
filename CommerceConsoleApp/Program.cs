// See https://aka.ms/new-console-template for more information
using CustomLogger.Buffering;
using CustomLogger.Configurations;
using CustomLogger.Providers;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.Serialization;

#region Logger

var options = new CustomProviderOptions
{
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
    .Build();

var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddProvider(provider);
});

var logger = loggerFactory.CreateLogger("Test");
#endregion

var activity = new Activity("CommerceConsoleAppActivity").Start();
var scopes = new Dictionary<string, object>
{
    ["traceId"] = activity.Context.TraceId.ToString(),
    ["spanId"] = activity.Context.SpanId.ToString()
};

#region Logs
using (logger.BeginScope(scopes))
{
    logger.LogInformation("CommerceConsoleApp iniciada com sucesso.");

    logger.LogTrace("This is a trace log, useful for debugging.");
    logger.LogDebug("This is a debug log, useful for development.");
    logger.LogInformation("This is an information log, useful for general information.");
    logger.LogWarning("This is a warning log, indicating a potential issue.");
    logger.LogError(new InvalidOperationException("Invalid Generic Operation"), "This is an error log, indicating a failure in the application.");
    logger.LogCritical("This is a critical log, indicating a severe failure.");
}
#endregion

provider.Dispose();

activity.Stop();
Thread.Sleep(1000);
