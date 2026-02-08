using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace CustomLogger.Configurations
{
    public sealed class LoggingOptions
    {
        public LogLevel? MinimumLogLevel { get; set; }
        public BufferOptions BufferOptions { get; set; }
        public BatchOptions BatchOptions { get; set; }
        public SinkOptions SinkOptions { get; set; }
    }

    public sealed class BufferOptions
    {
        public bool? Enabled { get; set; }
        public int? MaxSize { get; set; }
    }

    public sealed class BatchOptions
    {
        public int? BatchSize { get; set; }
        public int? FlushIntervalMs { get; set; }
    }

    public sealed class SinkOptions
    {
        public ConsoleSinkOptions Console { get; set; }
        public FileSinkOptions File { get; set; }
        public BlobStorageSinkOptions BlobStorage { get; set; }
    }

    public sealed class ConsoleSinkOptions
    {
        public bool? Enabled { get; set; }
    }

    public sealed class FileSinkOptions
    {
        public bool? Enabled { get; set; }
        public string Path { get; set; }
    }

    public sealed class BlobStorageSinkOptions
    {
        public bool? Enabled { get; set; }
        public string BlobName { get; set; }
        public string ConnectionString { get; set; }
        public string ContainerName { get; set; }
    }
}
