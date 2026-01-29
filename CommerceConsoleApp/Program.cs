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
    MinimumLogLevel = LogLevel.Trace,
    UseGlobalBuffer = true,
    MaxBufferSize = 1
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

using (logger.BeginScope(scopes))
{
    logger.LogInformation("CommerceConsoleApp iniciada com sucesso.");

    logger.LogTrace("This is a trace log, useful for debugging.");
    logger.LogDebug("This is a debug log, useful for development.");
    logger.LogInformation("This is an information log, useful for general information.");
    logger.LogWarning("This is a warning log, indicating a potential issue.");
    logger.LogError(new InvalidOperationException("Invalid Generic Operation"), "This is an error log, indicating a failure in the application.");
    logger.LogCritical("This is a critical log, indicating a severe failure.");
    // 1. State não-serializável
    logger.LogInformation("Test", new { DbContext = new object() });
    // ✅ Deve retornar: state: "AnonymousType"

    // 2. Exception com stack trace gigante
    logger.LogError(new StackOverflowException(), "Test");
    // ✅ Deve limitar a 10 linhas

}

activity.Stop();
Thread.Sleep(1000);
