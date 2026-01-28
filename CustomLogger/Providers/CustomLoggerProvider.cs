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
        private readonly ILogSink _sink;
        private bool _disposed;

        public CustomLoggerProvider(CustomProviderConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            var formatter = new JsonLogFormatter();
            _sink = new ConsoleLogSink(formatter);

            _buffer = new InstanceLogBuffer(_sink, _configuration.Options);
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
            if (_disposed) return;
            _disposed = true;

            _buffer.Flush();

            if (_sink is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
