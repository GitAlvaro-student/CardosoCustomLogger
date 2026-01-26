using CustomLogger.Abstractions;
using System.Text.Json;

namespace CustomLogger.Formatting
{
    public sealed class JsonLogFormatter : ILogFormatter
    {
        private static readonly JsonSerializerOptions _options =
            new JsonSerializerOptions
            {
                WriteIndented = false
            };

        public string Format(ILogEntry entry)
        {
            return JsonSerializer.Serialize(new
            {
                timestamp = entry.Timestamp,
                level = entry.LogLevel.ToString(),
                category = entry.Category,
                eventId = entry.EventId.Id,
                eventName = entry.EventId.Name,
                message = entry.Message,
                exception = entry.Exception?.ToString(),
                scopes = entry.Scopes,
                state = entry.State
            }, _options);
        }
    }
}
