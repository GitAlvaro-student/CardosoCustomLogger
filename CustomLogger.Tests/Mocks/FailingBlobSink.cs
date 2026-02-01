using CustomLogger.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomLogger.Tests.Mocks
{
    /// <summary>
    /// Simula BlobStorageLogSink com falha de conexão.
    /// Comportamento idêntico ao BlobStorageLogSink mas sempre falha no I/O.
    /// </summary>
    public sealed class FailingBlobSink : IAsyncBatchLogSink, IDisposable
    {
        private bool _disposed;

        public void Write(ILogEntry entry)
        {
            if (_disposed || entry == null)
                return;

            try
            {
                // Simula falha de conexão com Azure
                throw new Exception("Conexão com Blob Storage falhou");
            }
            catch
            {
                // Absorve falha - comportamento de best effort
            }
        }

        public void WriteBatch(IEnumerable<ILogEntry> entries)
        {
            if (_disposed || entries == null)
                return;

            try
            {
                throw new Exception("Conexão com Blob Storage falhou");
            }
            catch
            {
                // Absorve falha
            }
        }

        public async Task WriteAsync(ILogEntry entry, CancellationToken cancellationToken = default)
        {
            if (_disposed || entry == null)
                return;

            try
            {
                await Task.Run(() => throw new Exception("Conexão com Blob Storage falhou"), cancellationToken);
            }
            catch
            {
                // Absorve falha
            }
        }

        public async Task WriteBatchAsync(IEnumerable<ILogEntry> entries, CancellationToken cancellationToken = default)
        {
            if (_disposed || entries == null)
                return;

            try
            {
                await Task.Run(() => throw new Exception("Conexão com Blob Storage falhou"), cancellationToken);
            }
            catch
            {
                // Absorve falha
            }
        }

        public void Dispose()
        {
            _disposed = true;
        }
    }
}
