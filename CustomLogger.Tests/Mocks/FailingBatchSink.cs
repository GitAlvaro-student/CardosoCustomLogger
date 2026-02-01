using CustomLogger.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomLogger.Tests.Models
{
    public sealed class FailingBatchSink : IBatchLogSink
    {
        public List<ILogEntry> WrittenEntries { get; } = new();

        public void Write(ILogEntry entry)
        {
            // ✅ Write individual funciona
            WrittenEntries.Add(entry);
        }

        public void WriteBatch(IEnumerable<ILogEntry> entries)
        {
            // ❌ Batch sempre falha
            throw new Exception("Batch falhou");
        }
    }

}
