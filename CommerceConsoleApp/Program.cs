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

//#region OriginalLogs
//var options = new CustomProviderOptions
//{
//    UseGlobalBuffer = true,
//    BatchOptions = new BatchOptions
//    {
//        BatchSize = 5,
//        FlushInterval = TimeSpan.FromSeconds(5)
//    }
//};

//var provider = new CustomLoggerProviderBuilder()
//    .WithOptions(options)
//    .AddConsoleSink()
//    .AddFileSink("logs/app.log")
//    .AddBlobSink(connectionString, container)
//    .Build();

//var loggerFactory = LoggerFactory.Create(builder =>
//{
//    builder.AddProvider(provider);
//});

//var logger = loggerFactory.CreateLogger("Test");


//var activity = new Activity("CommerceConsoleAppActivity").Start();
//var scopes = new Dictionary<string, object>
//{
//    ["traceId"] = activity.Context.TraceId.ToString(),
//    ["spanId"] = activity.Context.SpanId.ToString()
//};

//using (logger.BeginScope(scopes))
//{
//    logger.LogInformation("CommerceConsoleApp iniciada com sucesso.");

//    logger.LogTrace("This is a trace log, useful for debugging.");
//    logger.LogDebug("This is a debug log, useful for development.");
//    logger.LogInformation("This is an information log, useful for general information.");
//    logger.LogWarning("This is a warning log, indicating a potential issue.");
//    logger.LogError(new InvalidOperationException("Invalid Generic Operation"), "This is an error log, indicating a failure in the application.");
//    logger.LogCritical("This is a critical log, indicating a severe failure.");
//}

//provider.Dispose();

//activity.Stop();
//Thread.Sleep(1000);

//#endregion

var options = new CustomProviderOptions
{
    UseGlobalBuffer = true,
    BackpressureOptions = new BackpressureOptions
    {
        MaxQueueCapacity = 5,
        OverflowStrategy = OverflowStrategy.DropOldest
    },
    BatchOptions = new BatchOptions
    {
        BatchSize = 100,  // Não vai fazer flush
        FlushInterval = TimeSpan.Zero
    }
};


#region Teste 1
var mockSink = new MockLogSink();
var buffer = new InstanceLogBuffer(mockSink, options);

// Enviar 10 logs (capacidade = 5)
for (int i = 0; i < 10; i++)
{
    buffer.Enqueue(new BufferedLogEntry(
        DateTimeOffset.UtcNow,
        "BackPressure",
        LogLevel.Error,
        new EventId(i, "DropOldest"),
        $"{i} - DropOldest Test",
        new InvalidOperationException(),
        null,
        null
    ));
}

buffer.Flush();

// ✅ ESPERADO:
// - Fila tem 5 logs (logs 5-9)
// - Logs 0-4 foram descartados
// - GetDroppedLogsCount() == 5

Assert.Equal(5, mockSink.WrittenEntries.Count);
Assert.Equal("Log 5", mockSink.WrittenEntries[0].Message);
Assert.Equal("Log 9", mockSink.WrittenEntries[4].Message);
Assert.Equal(5, buffer.GetDroppedLogsCount());
#endregion