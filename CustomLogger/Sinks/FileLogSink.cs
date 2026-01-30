using CustomLogger.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CustomLogger.Sinks
{
    public sealed class FileLogSink : ILogSink, IDisposable
    {
        private readonly ILogFormatter _formatter;
        private readonly StreamWriter _writer;
        private bool _disposed;

        public FileLogSink(
            string filePath,
            ILogFormatter formatter,
            bool append = true)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException(nameof(filePath));

            _formatter = formatter
                ?? throw new ArgumentNullException(nameof(formatter));

            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            _writer = new StreamWriter(
                new FileStream(
                    filePath,
                    append ? FileMode.Append : FileMode.Create,
                    FileAccess.Write,
                    FileShare.Read),
                Encoding.UTF8)
            {
                AutoFlush = true
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
        }
    }
}
