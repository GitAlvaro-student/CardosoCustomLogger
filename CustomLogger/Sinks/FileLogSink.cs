
using CustomLogger.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CustomLogger.Sinks
{
    public sealed class FileLogSink : IAsyncBatchLogSink, IDisposable
    {
        private readonly ILogFormatter _formatter;
        private readonly FileStream _fileStream;
        private readonly StreamWriter _writer;
        private bool _disposed;

        public FileLogSink(
            string filePath,
            ILogFormatter formatter,
            bool append = true)
        {
            _formatter = formatter
                ?? throw new ArgumentNullException(nameof(formatter));

            // Aplica nomenclatura padrão se filePath não for fornecido
            var resolvedPath = string.IsNullOrWhiteSpace(filePath)
                ? $"logs_{DateTimeOffset.UtcNow:yyyy-MM-dd}.log"
                : filePath;

            var directory = Path.GetDirectoryName(resolvedPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // ✅ FileStream com suporte assíncrono
            _fileStream = new FileStream(
                resolvedPath,
                append ? FileMode.Append : FileMode.Create,
                FileAccess.Write,
                FileShare.Read,
                4096,
                FileOptions.Asynchronous);  // ✅ Habilita I/O assíncrono

            _writer = new StreamWriter(_fileStream, Encoding.UTF8)
            {
                AutoFlush = false  // ✅ Flush manual para controle
            };
        }

        public void Write(ILogEntry entry)
        {
            if (_disposed || entry == null)
                return;

            try
            {
                var json = _formatter.Format(entry);
                _writer.WriteLine(json);
                _writer.Flush();
            }
            catch
            {
                // Absorve falha
            }
        }

        // ✅ NOVO: Escrita em lote
        public void WriteBatch(IEnumerable<ILogEntry> entries)
        {
            if (_disposed || entries == null)
                return;

            try
            {
                foreach (var entry in entries)
                {
                    var json = _formatter.Format(entry);
                    _writer.WriteLine(json);
                }
                _writer.Flush();  // ✅ Flush UMA VEZ para todo o batch
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
                var json = _formatter.Format(entry);
                await _writer.WriteLineAsync(json);
                await _writer.FlushAsync();
            }
            catch
            {
                // Absorve falha
            }
        }

        // ✅ NOVO: WriteBatch assíncrono
        public async Task WriteBatchAsync(IEnumerable<ILogEntry> entries, CancellationToken cancellationToken = default)
        {
            if (_disposed || entries == null)
                return;

            try
            {
                foreach (var entry in entries)
                {
                    var json = _formatter.Format(entry);
                    await _writer.WriteLineAsync(json);
                }
                await _writer.FlushAsync();  // ✅ Flush UMA VEZ
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

            _writer?.Dispose();
            _fileStream?.Dispose();
        }
    }
}
