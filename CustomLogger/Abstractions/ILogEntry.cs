using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace CustomLogger.Abstractions
{
    public interface ILogEntry
    {
        DateTimeOffset Timestamp { get; }
        string Category { get; }
        LogLevel LogLevel { get; }
        EventId EventId { get; }
        string Message { get; }
        Exception Exception { get; }
        object State { get; }
        IReadOnlyDictionary<string, object> Scopes { get; }
        string TraceId { get; }
        string SpanId { get; }
        string ParentSpanId { get; }
        string ServiceName { get; }
        string Environment { get; }
    }
}
