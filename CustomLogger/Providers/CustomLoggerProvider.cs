using CustomLogger.Abstractions;
using CustomLogger.Buffering;
using CustomLogger.Configurations;
using CustomLogger.Formatting;
using CustomLogger.Scopes;
using CustomLogger.Sinks;
using Microsoft.Extensions.Logging;
using System;

namespace CustomLogger.Providers
{
    /// <summary>
    /// Provider responsável por criar instâncias do CustomLogger
    /// e orquestrar o ciclo de vida do logging.
    /// </summary>
    public sealed class CustomLoggerProvider : ILoggerProvider
    {
        private readonly CustomProviderConfiguration _configuration;
        private readonly ILogBuffer _buffer;
        private bool _disposed;

        public CustomLoggerProvider(CustomProviderConfiguration configuration)
        {
            _configuration = configuration
                ?? throw new ArgumentNullException(nameof(configuration));

            var formatter = new JsonLogFormatter();

            var consoleSink = new ConsoleLogSink(formatter);
            
            var fileSink = new FileLogSink(
                "logs/app.log",
                formatter);
            
            var blobSink = new BlobStorageLogSink(
                "",
                "",
                "app-log.json",
                formatter);

            var sink = new CompositeLogSink(new ILogSink[]
            {
                consoleSink,
                fileSink,
                blobSink
            });

            GlobalLogBuffer.Configure(sink);

            // 4️⃣ Adapter que expõe o buffer global como ILogBuffer
            _buffer = new GlobalLogBufferAdapter(_configuration);
        }

        public ILogger CreateLogger(string categoryName)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(CustomLoggerProvider));

            var scopeProvider = new LogScopeProvider();

            return new Loggers.CustomLogger(
                categoryName,
                _configuration,
                _buffer,
                scopeProvider);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            _buffer.Flush();
            GlobalLogBuffer.Shutdown();
        }
    }
}
