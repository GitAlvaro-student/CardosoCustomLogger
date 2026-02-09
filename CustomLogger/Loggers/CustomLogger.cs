using CustomLogger.Abstractions;
using CustomLogger.Buffering;
using CustomLogger.Configurations;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace CustomLogger.Loggers
{
    /// <summary>
    /// Implementação customizada de ILogger.
    /// Responsável apenas por capturar e estruturar eventos de log.
    /// </summary>
    public sealed class CustomLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly CustomProviderConfiguration _configuration;
        private readonly IAsyncLogBuffer _buffer;
        private readonly ILogScopeProvider _logScopeProvider;

        public CustomLogger(
            string categoryName,
            CustomProviderConfiguration configuration,
            IAsyncLogBuffer buffer,
            ILogScopeProvider logScopeProvider)
        {
            _categoryName = categoryName
                ?? throw new ArgumentNullException(nameof(categoryName));

            _configuration = configuration
                ?? throw new ArgumentNullException(nameof(configuration));

            _buffer = buffer
                ?? throw new ArgumentNullException(nameof(buffer));
            _logScopeProvider = logScopeProvider;
        }

        /// <summary>
        /// Verifica se o nível de log está habilitado.
        /// </summary>
        public bool IsEnabled(LogLevel logLevel)
        {
            if (logLevel == LogLevel.None)
                return false;

            return logLevel >= _configuration.Options.MinimumLogLevel;
        }

        /// <summary>
        /// Inicia um escopo de log.
        /// Nesta implementação inicial, o escopo é ignorado.
        /// </summary>
        public IDisposable BeginScope<TState>(TState state)
        {
            return _logScopeProvider.Push(state);
        }
        /// <summary>
        /// Método central de logging.
        /// </summary>
        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception exception,
            Func<TState, Exception, string> formatter)
        {
            Debug.WriteLine("[Log<TState>] >>> Iniciando Log<TState>");

            if (!IsEnabled(logLevel))
                return;

            if (formatter == null)
                throw new ArgumentNullException(nameof(formatter));

            var message = formatter(state, exception);

            // Neste ponto, apenas estruturamos o evento.
            // A escrita real será responsabilidade do buffer/sink futuramente.
            var entry = new BufferedLogEntry(
                DateTimeOffset.UtcNow,
                _categoryName,
                logLevel,
                eventId,
                message,
                exception,
                state,
                _logScopeProvider.GetScopes()
            );

            _buffer.EnqueueAsync(entry);


            // Ponto de extensão:
            // Aqui futuramente o log será enviado para buffer ou sink
            // Ex: LogDispatcher.Dispatch(logEntry);
        }
    }
}
