using CustomLogger.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomLogger.Tests.Mocks
{
    public sealed class MockLogBuffer : ILogBuffer
    {
        public List<ILogEntry> EnqueuedEntries { get; } = new();

        public void Enqueue(ILogEntry entry)
        {
            EnqueuedEntries.Add(entry);
        }

        public void Flush()
        {
            // Não faz nada - apenas para satisfazer contrato
        }
    }
}
