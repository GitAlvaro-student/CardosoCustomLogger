using CustomLogger.Configurations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Net.NetworkInformation;

namespace CustomLogger.Adapters
{
    public sealed class CoreConfigurationAdapter
    {
        public LoggingOptions CreateFromConfiguration(IConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            var section = configuration.GetSection("CustomLogger");

            // MinimumLogLevel
            LogLevel? minimumLogLevel = null;
            var levelStr = section["MinimumLogLevel"];
            if (!string.IsNullOrWhiteSpace(levelStr))
            {
                if (Enum.TryParse<LogLevel>(levelStr, ignoreCase: true, out var parsedLevel))
                {
                    minimumLogLevel = parsedLevel;
                }
            }

            // BufferOptions
            BufferOptions bufferOptions = null;
            var bufferSection = section.GetSection("Buffer");
            if (bufferSection.Exists())
            {
                bool? bufferEnabled = null;
                var enabledValue = bufferSection["Enabled"];
                if (!string.IsNullOrWhiteSpace(enabledValue) && bool.TryParse(enabledValue, out var parsedEnabled))
                {
                    bufferEnabled = parsedEnabled;
                }

                int? maxSize = null;
                var maxSizeValue = bufferSection["MaxSize"];
                if (!string.IsNullOrWhiteSpace(maxSizeValue) && int.TryParse(maxSizeValue, out var parsedMaxSize))
                {
                    maxSize = parsedMaxSize;
                }

                bufferOptions = new BufferOptions(bufferEnabled, maxSize);
            }

            // SinkOptions
            SinkOptions sinkOptions = null;
            var sinksSection = section.GetSection("Sinks");
            if (sinksSection.Exists())
            {
                // Console
                ConsoleSinkOptions consoleOptions = null;
                var consoleSection = sinksSection.GetSection("Console");
                if (consoleSection.Exists())
                {
                    bool? consoleEnabled = null;
                    var consoleEnabledValue = consoleSection["Enabled"];
                    if (!string.IsNullOrWhiteSpace(consoleEnabledValue) && bool.TryParse(consoleEnabledValue, out var parsedConsoleEnabled))
                    {
                        consoleEnabled = parsedConsoleEnabled;
                    }

                    consoleOptions = new ConsoleSinkOptions(consoleEnabled);
                }

                // File
                FileSinkOptions fileOptions = null;
                var fileSection = sinksSection.GetSection("File");
                if (fileSection.Exists())
                {
                    bool? fileEnabled = null;
                    var fileEnabledValue = fileSection["Enabled"];
                    if (!string.IsNullOrWhiteSpace(fileEnabledValue) && bool.TryParse(fileEnabledValue, out var parsedFileEnabled))
                    {
                        fileEnabled = parsedFileEnabled;
                    }

                    var filePath = fileSection["Path"];

                    fileOptions = new FileSinkOptions(fileEnabled, filePath);
                }

                // BlobStorage
                BlobStorageSinkOptions blobOptions = null;
                var blobSection = sinksSection.GetSection("BlobStorage");
                if (blobSection.Exists())
                {
                    bool? blobEnabled = null;
                    var blobEnabledValue = blobSection["Enabled"];
                    if (!string.IsNullOrWhiteSpace(blobEnabledValue) && bool.TryParse(blobEnabledValue, out var parsedBlobEnabled))
                    {
                        blobEnabled = parsedBlobEnabled;
                    }

                    var connectionString = blobSection["ConnectionString"];
                    var containerName = blobSection["ContainerName"];

                    blobOptions = new BlobStorageSinkOptions(
                        blobEnabled,
                        connectionString,
                        containerName
                    );
                }

                // Dynatrace
                DynatraceSinkOptions dynatraceOptions = null;
                var dynatraceSection = sinksSection.GetSection("Dynatrace");
                if (dynatraceSection.Exists())
                {
                    bool? dynatraceEnabled = null;
                    var dynatraceEnabledValue = dynatraceSection["Enabled"];
                    if (!string.IsNullOrWhiteSpace(dynatraceEnabledValue) &&
                        bool.TryParse(dynatraceEnabledValue, out var parsedDynatraceEnabled))
                    {
                        dynatraceEnabled = parsedDynatraceEnabled;
                    }

                    var endpoint = dynatraceSection["Endpoint"];
                    var apiToken = dynatraceSection["ApiToken"];

                    int? timeoutSeconds = null;
                    var timeoutValue = dynatraceSection["TimeoutSeconds"];
                    if (!string.IsNullOrWhiteSpace(timeoutValue) &&
                        int.TryParse(timeoutValue, out var parsedTimeout))
                    {
                        timeoutSeconds = parsedTimeout;
                    }

                    dynatraceOptions = new DynatraceSinkOptions(
                        dynatraceEnabled,
                        endpoint,
                        apiToken,
                        timeoutSeconds
                    );
                }


                sinkOptions = new SinkOptions(
                    consoleOptions,
                    fileOptions,
                    blobOptions,
                    dynatraceOptions
                );
            }

            BatchOptions batchOptions = null;
            var batchSection = section.GetSection("Batch");
            if (batchSection.Exists())
            {
                int? batchSize = null;
                var batchSizeValue = batchSection["BatchSize"];
                if (!string.IsNullOrWhiteSpace(batchSizeValue) && int.TryParse(batchSizeValue, out var parsedBatchSize))
                {
                    batchSize = parsedBatchSize;
                }

                int? flushIntervalMs = null;
                var flushIntervalValue = batchSection["FlushIntervalMs"];
                if (!string.IsNullOrWhiteSpace(flushIntervalValue) && int.TryParse(flushIntervalValue, out var parsedFlushInterval))
                {
                    flushIntervalMs = parsedFlushInterval;
                }

                batchOptions = new BatchOptions(batchSize, flushIntervalMs);
            }

            return new LoggingOptions(
                    minimumLogLevel,
                    bufferOptions,
                    batchOptions,
                    sinkOptions
                );
        }
    }
}
