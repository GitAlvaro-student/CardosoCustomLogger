using CustomLogger.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomLogger.Tests.Mocks
{
    /// <summary>
    /// Falha após N escritas (simula disco cheio, conexão cair, etc.)
    /// </summary>
    public sealed class FailAfterNSink : ILogSink, IBatchLogSink
    {
        private readonly int _failAfter;
        private int _count;
        private readonly object _lock = new object();
        public List<ILogEntry> WrittenEntries { get; } = new();

        public FailAfterNSink(int failAfter)
        {
            _failAfter = failAfter;
        }

        public void Write(ILogEntry entry)
        {
            lock (_lock)
            {
                _count++;
                if (_count > _failAfter)
                    throw new Exception($"Falha após {_failAfter} escritas");

                WrittenEntries.Add(entry);
            }
        }

        public void WriteBatch(IEnumerable<ILogEntry> entries)
        {
            lock (_lock)
            {
                foreach (var entry in entries)
                {
                    _count++;
                    if (_count > _failAfter)
                        throw new Exception($"Falha após {_failAfter} escritas");

                    WrittenEntries.Add(entry);
                }
            }
        }
    }
}
