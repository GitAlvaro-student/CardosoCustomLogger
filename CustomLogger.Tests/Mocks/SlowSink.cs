using CustomLogger.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomLogger.Tests.Mocks
{
    /// <summary>
    /// Simula sink lento (ex: Blob Storage com latência)
    /// </summary>
    public sealed class SlowSink : ILogSink, IBatchLogSink
    {
        private readonly int _delayMs;
        private readonly object _lock = new object();
        public List<ILogEntry> WrittenEntries { get; } = new();

        public SlowSink(int delayMs = 500)
        {
            _delayMs = delayMs;
        }

        public void Write(ILogEntry entry)
        {
            Thread.Sleep(_delayMs);
            lock (_lock)
            {
                WrittenEntries.Add(entry);
            }
        }

        public void WriteBatch(IEnumerable<ILogEntry> entries)
        {
            Thread.Sleep(_delayMs);
            lock (_lock)
            {
                WrittenEntries.AddRange(entries);
            }
        }
    }
}
