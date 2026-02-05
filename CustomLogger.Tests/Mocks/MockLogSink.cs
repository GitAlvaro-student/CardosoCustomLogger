using CustomLogger.Abstractions;
using System.Collections.Generic;

namespace CustomLogger.Tests.Mocks
{

    /// <summary>
    /// Thread-safe para testes de pipeline com concorrência
    /// </summary>
    public sealed class MockLogSink : ILogSink, IBatchLogSink
    {
        private readonly object _lock = new object();
        public List<ILogEntry> WrittenEntries { get; } = new();

        public void Write(ILogEntry entry)
        {
            lock (_lock)
            {
                WrittenEntries.Add(entry);
            }
        }

        public void WriteBatch(IEnumerable<ILogEntry> entries)
        {
            lock (_lock)
            {
                WrittenEntries.AddRange(entries);
            }
        }
    }
}
