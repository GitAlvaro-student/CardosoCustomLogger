// See https://aka.ms/new-console-template for more information
using CustomLogger.Buffering;
using CustomLogger.Configurations;
using CustomLogger.Providers;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

#region Logger

var options = new CustomProviderOptions
{
    MinimumLogLevel = LogLevel.Trace,
    UseGlobalBuffer = true,
    MaxBufferSize = 50
};

var provider = new CustomLoggerProviderBuilder()
    .WithOptions(options)
    .AddConsoleSink()
    .AddFileSink("logs/app.log")
    .AddBlobSink("", "", "app-log.json")  // Strings vazias OK para validação
    .Build();

var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddProvider(provider);
    //builder.AddCustomLogging(
    //    opts => opts.MinimumLogLevel = LogLevel.Information,
    //    sinks => sinks.WithOptions(options).AddConsoleSink()
    //    .AddFileSink("app.log")
    //    );
});

var logger = loggerFactory.CreateLogger("Test");
#endregion

var activity = new Activity("CommerceConsoleAppActivity").Start();
var scopes = new Dictionary<string, object>
{
    ["traceId"] = activity.Context.TraceId.ToString(),
    ["spanId"] = activity.Context.SpanId.ToString()
};

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

activity.Stop();
Thread.Sleep(1000);
