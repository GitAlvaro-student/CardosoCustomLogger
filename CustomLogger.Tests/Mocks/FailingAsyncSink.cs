using CustomLogger.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CustomLogger.Tests.Mocks
{
    public sealed class FailingAsyncSink : IAsyncBatchLogSink, IDisposable
    {
        private bool _disposed;

        public void Write(ILogEntry entry)
        {
            throw new Exception("Write síncrono falhou");
        }

        public void WriteBatch(IEnumerable<ILogEntry> entries)
        {
            throw new Exception("WriteBatch síncrono falhou");
        }

        public async Task WriteAsync(ILogEntry entry, CancellationToken cancellationToken = default)
        {
            await Task.Run(() => throw new Exception("WriteAsync falhou"), cancellationToken);
        }

        public async Task WriteBatchAsync(IEnumerable<ILogEntry> entries, CancellationToken cancellationToken = default)
        {
            await Task.Run(() => throw new Exception("WriteBatchAsync falhou"), cancellationToken);
        }

        public void Dispose() { _disposed = true; }
    }
}
