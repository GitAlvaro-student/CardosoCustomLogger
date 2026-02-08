using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace CustomLogger.Configurations
{
    public sealed class LoggingOptions
    {
        /// <summary>
        /// Nível mínimo de log. Logs abaixo deste nível serão ignorados.
        /// Padrão: Information (se não especificado)
        /// </summary>
        public LogLevel? MinimumLogLevel { get; }

        /// <summary>
        /// Configurações do buffer de logs em memória.
        /// Padrão: Enabled=true, MaxSize=50
        /// </summary>
        public BufferOptions BufferOptions { get; }

        /// <summary>
        /// Configurações de Batch de logs para envio.
        /// Padrão: Enabled=true, BatchSize=30, FlushIntervalMs=5000
        /// </summary>
        public BatchOptions BatchOptions { get; }

        /// <summary>
        /// Configurações de Batch de logs para envio.
        /// Padrão: ConsoleLogSink.Enabled=true
        /// </summary>
        public SinkOptions SinkOptions { get; }

        public LoggingOptions(
        LogLevel? minimumLogLevel = null,
        BufferOptions bufferOptions = null,
        BatchOptions batchOptions = null,
        SinkOptions sinkOptions = null)
        {
            MinimumLogLevel = minimumLogLevel;
            BufferOptions = bufferOptions;
            BatchOptions = batchOptions;
            SinkOptions = sinkOptions;
        }
    }

    public sealed class BufferOptions
    {
        public bool? Enabled { get; }
        public int? MaxSize { get; }

        public BufferOptions(bool? enabled, int? maxSize)
        {
            Enabled = enabled;
            MaxSize = maxSize;
        }
    }

    public sealed class BatchOptions
    {
        public int? BatchSize { get; }
        public int? FlushIntervalMs { get; }
        public BatchOptions(int? batchSize, int? flushIntervalMs)
        {
            BatchSize = batchSize;
            FlushIntervalMs = flushIntervalMs;
        }
    }

    public sealed class SinkOptions
    {
        public ConsoleSinkOptions Console { get; }
        public FileSinkOptions File { get; }
        public BlobStorageSinkOptions BlobStorage { get; }
        public SinkOptions(ConsoleSinkOptions console, FileSinkOptions file, BlobStorageSinkOptions blobStorage)
        {
            Console = console;
            File = file;
            BlobStorage = blobStorage;
        }
    }

    public sealed class ConsoleSinkOptions
    {
        public bool? Enabled { get; }
        public ConsoleSinkOptions(bool? enabled)
        {
            Enabled = enabled;
        }
    }

    public sealed class FileSinkOptions
    {
        public bool? Enabled { get; }
        public string Path { get; }
        public FileSinkOptions(bool? enabled, string path)
        {
            Enabled = enabled;
            Path = path;
        }
    }

    public sealed class BlobStorageSinkOptions
    {
        public bool? Enabled { get; }
        public string BlobName { get; }
        public string ConnectionString { get; }
        public string ContainerName { get; }
        public BlobStorageSinkOptions(bool? enabled, string connectionString, string containerName)
        {
            Enabled = enabled;
            ConnectionString = connectionString;
            ContainerName = containerName;
        }
    }
}
