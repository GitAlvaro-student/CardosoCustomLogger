using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace CustomLogger.Models
{
    /// <summary>
    /// Representa um evento de log estruturado em memória.
    /// </summary>
    public sealed class LogEntry
    {
        public DateTimeOffset Timestamp { get; set; }
        public string Category { get; set; }
        public LogLevel LogLevel { get; set; }
        public EventId EventId { get; set; }
        public string Message { get; set; }
        public Exception Exception { get; set; }
        public object State { get; set; }
    }
}
