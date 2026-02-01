using CustomLogger.Abstractions;
using CustomLogger.Buffering;
using CustomLogger.Configurations;
using CustomLogger.Formatting;
using CustomLogger.Scopes;
using CustomLogger.Sinks;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CustomLogger.Providers
{
    /// <summary>
    /// Provider responsável por criar instâncias do CustomLogger
    /// e orquestrar o ciclo de vida do logging.
    /// </summary>
    public sealed class CustomLoggerProvider : ILoggerProvider
    {
        private readonly CustomProviderConfiguration _configuration;
        private readonly IAsyncLogBuffer _buffer;
        private readonly List<IDisposable> _disposables = new List<IDisposable>();
        private bool _disposed;

        // Construtor público para uso com builder
        public CustomLoggerProvider(
            CustomProviderOptions options,
            ILogSink sink,
            IEnumerable<ILogSink> sinksToTrack)
        {
            _configuration = new CustomProviderConfiguration(options);
            _buffer = new InstanceLogBuffer(sink, options);

            // Rastreia sinks descartáveis
            foreach (var s in sinksToTrack)
            {
                if (s is IDisposable disposable)
                    _disposables.Add(disposable);
            }
        }

        public ILogger CreateLogger(string categoryName)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(CustomLoggerProvider));

            var scopeProvider = new LogScopeProvider();
            return new Loggers.CustomLogger(categoryName, _configuration, _buffer, scopeProvider);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            // ✅ 1. Flush do buffer PRIMEIRO
            _buffer.Flush();

            // ✅ 2. Dispose do buffer (para timer)
            if (_buffer is IDisposable disposableBuffer)
            {
                disposableBuffer.Dispose();
            }

            // ✅ 3. Dispose dos sinks POR ÚLTIMO
            foreach (var disposable in _disposables)
            {
                try { disposable.Dispose(); }
                catch { }
            }
        }
    }
}
