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
        public BufferedLogEntry(DateTimeOffset timestamp, string category, LogLevel logLevel, EventId eventId,
            string message, Exception exception, object state, IReadOnlyDictionary<string, object> scopes,
            string traceId = null, string spanId = null, string parentSpanId = null, string serviceName = null,
            string environment = null, string httpMethod = null, string httpPath = null,
            int? httpStatusCode = null, long? httpDurationMs = null, string clientIpAddress = null,
            string serverIpAddress = null)
        {
            Timestamp = timestamp;
            Category = category;
            LogLevel = logLevel;
            EventId = eventId;
            Message = message;
            Exception = exception;
            State = state;
            Scopes = scopes ?? new Dictionary<string, object>();
            TraceId = traceId;
            SpanId = spanId;
            ParentSpanId = parentSpanId;
            ServiceName = serviceName;
            Environment = environment;
            HttpMethod = httpMethod;
            HttpPath = httpPath;
            HttpStatusCode = httpStatusCode;
            HttpDurationMs = httpDurationMs;
            ClientIpAddress = clientIpAddress;
            ServerIpAddress = serverIpAddress;
            HttpMethod = httpMethod;
            HttpPath = httpPath;
            HttpStatusCode = httpStatusCode;
            HttpDurationMs = httpDurationMs;
            ClientIpAddress = clientIpAddress;
            ServerIpAddress = serverIpAddress;
        }

        public DateTimeOffset Timestamp { get; set; }

        public string Category { get; set; }

        public LogLevel LogLevel { get; set; }

        public EventId EventId { get; set; }

        public string Message { get; set; }

        public Exception Exception { get; set; }

        public object State { get; set; }

        public IReadOnlyDictionary<string, object> Scopes { get; }
        public string TraceId { get; }
        public string SpanId { get; }
        public string ParentSpanId { get; }
        public string ServiceName { get; }
        public string Environment { get; }
        public string HttpMethod { get; }
        public string HttpPath { get; }
        public int? HttpStatusCode { get; }
        public long? HttpDurationMs { get; }
        public string ClientIpAddress { get; }
        public string ServerIpAddress { get; }

    }
}
