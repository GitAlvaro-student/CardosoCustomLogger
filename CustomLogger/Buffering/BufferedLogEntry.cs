using CustomLogger.Abstractions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace CustomLogger.Buffering
{
    /// <summary>
    /// Representa um evento de log estruturado armazenado no buffer.
    /// </summary>
    public sealed class BufferedLogEntry : ILogEntry
    {
        public BufferedLogEntry(DateTimeOffset timestamp, string category, LogLevel logLevel, EventId eventId, string message, Exception exception, object state, IReadOnlyDictionary<string, object> scopes)
        {
            Timestamp = timestamp;
            Category = category;
            LogLevel = logLevel;
            EventId = eventId;
            Message = message;
            Exception = exception;
            State = state;
            Scopes = scopes ?? new Dictionary<string, object>();
        }

        public DateTimeOffset Timestamp { get; set; }

        public string Category { get; set; }

        public LogLevel LogLevel { get; set; }

        public EventId EventId { get; set; }

        public string Message { get; set; }

        public Exception Exception { get; set; }

        public object State { get; set; }

        public IReadOnlyDictionary<string, object> Scopes { get; }

    }
}
