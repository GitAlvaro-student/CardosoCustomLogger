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

//using (logger.BeginScope(new { UserId = "123" }))      // Externo
//using (logger.BeginScope(new { UserId = "456" }))      // Interno
//{
//    logger.LogInformation("Test");
//    // ✅ Resultado: UserId = "456" (interno prevalece)
//}

// Thread A
//using (logger.BeginScope(new { Request = "A" }))
//{
//    await Task.Delay(100);
//    logger.LogInformation("A");  // ✅ Request = "A"
//}

//// Thread B (paralelo)
//using (logger.BeginScope(new { Request = "B" }))
//{
//    logger.LogInformation("B");  // ✅ Request = "B" (isolado)
//}

var logger1 = loggerFactory.CreateLogger("Test1");
var logger2 = loggerFactory.CreateLogger("Test2");

// Escopo apenas para logger1
using (logger1.BeginScope(new { Feature = "A" }))
{
    logger1.LogInformation("Msg1");  // ✅ Deve ter Feature = "A"
    logger2.LogInformation("Msg2");  // ❌ NÃO deve ter Feature
}
activity.Stop();
Thread.Sleep(1000);
