using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using CustomLogger.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CustomLogger.Sinks
{
    public sealed class BlobStorageLogSink : IAsyncBatchLogSink, IDisposable
    {
        private readonly ILogFormatter _formatter;
        private readonly AppendBlobClient _blobClient;
        private bool _disposed;

        public BlobStorageLogSink(
            string connectionString,
            string containerName,
            ILogFormatter formatter,
            string blobName = null)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException(nameof(connectionString));

            if (string.IsNullOrWhiteSpace(containerName))
                throw new ArgumentException(nameof(containerName));

            _formatter = formatter
                ?? throw new ArgumentNullException(nameof(formatter));

            // Aplica nomenclatura padrão se blobName não for fornecido
            var resolvedBlobName = string.IsNullOrWhiteSpace(blobName)
                ? $"logs_{DateTimeOffset.UtcNow:yyyy-MM-dd}.log"
                : blobName;

            var containerClient = new BlobContainerClient(
                connectionString,
                containerName);

            containerClient.CreateIfNotExists();

            var appendBlobClient =
                containerClient.GetAppendBlobClient(resolvedBlobName);

            appendBlobClient.CreateIfNotExists();

            _blobClient = appendBlobClient;
        }

        public void Write(ILogEntry entry)
        {
            if (entry == null) return;

            try
            {
                var json = _formatter.Format(entry) + Environment.NewLine;
                var bytes = Encoding.UTF8.GetBytes(json);

                using (var stream = new MemoryStream(bytes))
                {
                    _blobClient.AppendBlock(stream);
                }
            }
            catch
            {
                // Absorve falha
            }
        }

        // ✅ Método síncrono batch
        public void WriteBatch(IEnumerable<ILogEntry> entries)
        {
            if (_disposed || entries == null)
                return;

            try
            {
                using (var stream = new MemoryStream())
                {
                    foreach (var entry in entries)
                    {
                        var json = _formatter.Format(entry) + Environment.NewLine;
                        var bytes = Encoding.UTF8.GetBytes(json);
                        stream.Write(bytes, 0, bytes.Length);
                    }

                    stream.Position = 0;
                    _blobClient.AppendBlock(stream);  // ✅ UMA chamada para todo o batch
                }
            }
            catch
            {
                // Absorve falha
            }
        }
        // ✅ NOVO: Write assíncrono
        public async Task WriteAsync(ILogEntry entry, CancellationToken cancellationToken = default)
        {
            if (_disposed || entry == null)
                return;

            try
            {
                var json = _formatter.Format(entry) + Environment.NewLine;
                var bytes = Encoding.UTF8.GetBytes(json);

                using (var stream = new MemoryStream(bytes))
                {
                    await _blobClient.AppendBlockAsync(stream, cancellationToken: cancellationToken);
                }
            }
            catch
            {
                // Absorve falha
            }
        }

        // ✅ NOVO: WriteBatch assíncrono (MELHOR para Blob Storage)
        public async Task WriteBatchAsync(IEnumerable<ILogEntry> entries, CancellationToken cancellationToken = default)
        {
            if (_disposed || entries == null)
                return;

            try
            {
                using (var stream = new MemoryStream())
                {
                    foreach (var entry in entries)
                    {
                        var json = _formatter.Format(entry) + Environment.NewLine;
                        var bytes = Encoding.UTF8.GetBytes(json);
                        await stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
                    }

                    stream.Position = 0;

                    // ✅ UMA chamada de rede para todo o batch (eficiente!)
                    await _blobClient.AppendBlockAsync(stream, cancellationToken: cancellationToken);
                }
            }
            catch
            {
                // Absorve falha
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            // AppendBlobClient não precisa de dispose
        }
    }
}
