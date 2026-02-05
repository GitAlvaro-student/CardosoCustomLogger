using System;
using System.Collections.Generic;
using System.Text;

namespace CustomLogger.Configurations
{
    public sealed class CustomLoggerConfiguration
    {
        public string MinimumLogLevel { get; set; }
        public BufferConfiguration Buffer { get; set; }
        public SinkConfigurations Sinks { get; set; }
    }

    public sealed class BufferConfiguration
    {
        public bool Enabled { get; set; }
        public int MaxSize { get; set; }
        public int FlushIntervalMs { get; set; }
    }

    public sealed class SinkConfigurations
    {
        public ConsoleSinkConfiguration Console { get; set; }
        public FileSinkConfiguration File { get; set; }
        public BlobStorageSinkConfiguration BlobStorage { get; set; }
    }

    public sealed class ConsoleSinkConfiguration
    {
        public bool Enabled { get; set; }
    }

    public sealed class FileSinkConfiguration
    {
        public bool Enabled { get; set; }
        public string Path { get; set; }
    }

    public sealed class BlobStorageSinkConfiguration
    {
        public bool Enabled { get; set; }
        public string ConnectionString { get; set; }
        public string ContainerName { get; set; }
    }
}
