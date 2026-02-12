using CustomLogger.Abstractions;
using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CustomLogger.Formatting
{
    public sealed class JsonLogFormatter : ILogFormatter
    {
        private static readonly JsonSerializerOptions _options = new JsonSerializerOptions
        {
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public string Format(ILogEntry entry)
        {
            if (entry == null)
                return "{}";

            try
            {
                return JsonSerializer.Serialize(new
                {
                    timestamp = entry.Timestamp,
                    level = entry.LogLevel.ToString(),
                    category = entry.Category,
                    eventId = entry.EventId.Id,
                    eventName = entry.EventId.Name,
                    message = entry.Message,
                    exception = FormatException(entry.Exception),
                    scopes = entry.Scopes,
                    state = FormatState(entry.State),
                    traceId = entry.TraceId,
                    spanId = entry.SpanId,
                    parentSpanId = entry.ParentSpanId,
                    serviceName = entry.ServiceName,
                    environment = entry.Environment,
                }, _options);
            }
            catch
            {
                // ✅ Fallback: JSON mínimo válido
                return JsonSerializer.Serialize(new
                {
                    timestamp = DateTimeOffset.UtcNow,
                    level = "Error",
                    message = "[FORMATTER-ERROR] Failed to serialize log entry"
                }, _options);
            }
        }

        private static object FormatState(object state)
        {
            if (state == null)
                return null;

            try
            {
                // Tenta serializar para validar
                JsonSerializer.Serialize(state, _options);
                return state;
            }
            catch
            {
                // ✅ Fallback seguro
                return state.GetType().Name;
            }
        }

        private static object FormatException(Exception exception)
        {
            if (exception == null)
                return null;

            try
            {
                // ✅ Formato estruturado e limitado
                return new
                {
                    type = exception.GetType().FullName,
                    message = exception.Message,
                    stackTrace = TruncateStackTrace(exception.StackTrace),
                    innerException = exception.InnerException?.Message
                };
            }
            catch
            {
                // ✅ Fallback mínimo
                return exception.Message ?? exception.GetType().Name;
            }
        }

        private static string TruncateStackTrace(string stackTrace)
        {
            if (string.IsNullOrEmpty(stackTrace))
                return null;

            // ✅ Limita a 10 primeiras linhas
            var lines = stackTrace.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var limited = lines.Take(10);
            return string.Join(Environment.NewLine, limited);
        }
    }
}
