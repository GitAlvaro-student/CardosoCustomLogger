using CustomLogger.Abstractions;

namespace CustomLogger.Tests.Models
{
    public sealed class MockLogSink : ILogSink, IBatchLogSink
    {
        public List<ILogEntry> WrittenEntries { get; } = new List<ILogEntry>();

        public void Write(ILogEntry entry)
        {
            WrittenEntries.Add(entry);
        }

        public void WriteBatch(IEnumerable<ILogEntry> entries)
        {
            WrittenEntries.AddRange(entries);
        }
    }
}
