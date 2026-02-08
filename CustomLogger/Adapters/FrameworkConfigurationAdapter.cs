using CustomLogger.Configurations;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;

namespace CustomLogger.Adapters
{
    internal sealed class NetFrameworkConfigurationAdapter
    {
        public LoggingOptions CreateFromAppSettings()
        {
            var appSettings = ConfigurationManager.AppSettings;

            // MinimumLogLevel
            LogLevel? minimumLogLevel = null;
            var levelKey = "CustomLogger:MinimumLogLevel";
            var levelStr = appSettings[levelKey];
            if (!string.IsNullOrWhiteSpace(levelStr))
            {
                if (Enum.TryParse<LogLevel>(levelStr, ignoreCase: true, out var parsedLevel))
                {
                    minimumLogLevel = parsedLevel;
                }
                else
                {
                    throw new ConfigurationErrorsException($"Valor inválido para '{levelKey}': '{levelStr}'. Esperado um valor válido de LogLevel.");
                }
            }

            // BufferOptions
            BufferOptions bufferOptions = null;

            var bufferEnabledKey = "CustomLogger:Buffer:Enabled";
            var bufferEnabledValue = appSettings[bufferEnabledKey];
            bool? bufferEnabled = null;
            if (!string.IsNullOrWhiteSpace(bufferEnabledValue))
            {
                if (bool.TryParse(bufferEnabledValue, out var parsedEnabled))
                {
                    bufferEnabled = parsedEnabled;
                }
                else
                {
                    throw new ConfigurationErrorsException($"Valor inválido para '{bufferEnabledKey}': '{bufferEnabledValue}'. Esperado 'true' ou 'false'.");
                }
            }

            var maxSizeKey = "CustomLogger:Buffer:MaxSize";
            var maxSizeValue = appSettings[maxSizeKey];
            int? maxSize = null;
            if (!string.IsNullOrWhiteSpace(maxSizeValue))
            {
                if (int.TryParse(maxSizeValue, out var parsedMaxSize))
                {
                    maxSize = parsedMaxSize;
                }
                else
                {
                    throw new ConfigurationErrorsException($"Valor inválido para '{maxSizeKey}': '{maxSizeValue}'. Esperado um número inteiro.");
                }
            }

            if (bufferEnabled.HasValue || maxSize.HasValue)
            {
                bufferOptions = new BufferOptions(bufferEnabled, maxSize);
            }

            // SinkOptions
            SinkOptions sinkOptions = null;

            // Console
            ConsoleSinkOptions consoleOptions = null;
            var consoleEnabledKey = "CustomLogger:Sinks:Console:Enabled";
            var consoleEnabledValue = appSettings[consoleEnabledKey];
            bool? consoleEnabled = null;
            if (!string.IsNullOrWhiteSpace(consoleEnabledValue))
            {
                if (bool.TryParse(consoleEnabledValue, out var parsedConsoleEnabled))
                {
                    consoleEnabled = parsedConsoleEnabled;
                }
                else
                {
                    throw new ConfigurationErrorsException($"Valor inválido para '{consoleEnabledKey}': '{consoleEnabledValue}'. Esperado 'true' ou 'false'.");
                }

                consoleOptions = new ConsoleSinkOptions(consoleEnabled);
            }

            // File
            FileSinkOptions fileOptions = null;
            var fileEnabledKey = "CustomLogger:Sinks:File:Enabled";
            var fileEnabledValue = appSettings[fileEnabledKey];
            bool? fileEnabled = null;
            if (!string.IsNullOrWhiteSpace(fileEnabledValue))
            {
                if (bool.TryParse(fileEnabledValue, out var parsedFileEnabled))
                {
                    fileEnabled = parsedFileEnabled;
                }
                else
                {
                    throw new ConfigurationErrorsException($"Valor inválido para '{fileEnabledKey}': '{fileEnabledValue}'. Esperado 'true' ou 'false'.");
                }
            }

            var filePathKey = "CustomLogger:Sinks:File:Path";
            var filePath = appSettings[filePathKey];

            if (fileEnabled.HasValue || !string.IsNullOrWhiteSpace(filePath))
            {
                fileOptions = new FileSinkOptions(fileEnabled, filePath);
            }

            // BlobStorage
            BlobStorageSinkOptions blobOptions = null;
            var blobEnabledKey = "CustomLogger:Sinks:BlobStorage:Enabled";
            var blobEnabledValue = appSettings[blobEnabledKey];
            bool? blobEnabled = null;
            if (!string.IsNullOrWhiteSpace(blobEnabledValue))
            {
                if (bool.TryParse(blobEnabledValue, out var parsedBlobEnabled))
                {
                    blobEnabled = parsedBlobEnabled;
                }
                else
                {
                    throw new ConfigurationErrorsException($"Valor inválido para '{blobEnabledKey}': '{blobEnabledValue}'. Esperado 'true' ou 'false'.");
                }
            }

            var connectionStringKey = "CustomLogger:Sinks:BlobStorage:ConnectionString";
            var connectionString = appSettings[connectionStringKey];

            var containerNameKey = "CustomLogger:Sinks:BlobStorage:ContainerName";
            var containerName = appSettings[containerNameKey];

            if (blobEnabled.HasValue || !string.IsNullOrWhiteSpace(connectionString) || !string.IsNullOrWhiteSpace(containerName))
            {
                blobOptions = new BlobStorageSinkOptions(blobEnabled, connectionString, containerName);
            }

            if (consoleOptions != null || fileOptions != null || blobOptions != null)
            {
                sinkOptions = new SinkOptions(consoleOptions, fileOptions, blobOptions);
            }

            BatchOptions batchOptions = null;

            var batchSizeKey = "CustomLogger:Batch:BatchSize";
            var batchSizeValue = appSettings[batchSizeKey];
            int? batchSize = null;
            if (!string.IsNullOrWhiteSpace(batchSizeValue))
            {
                if (int.TryParse(batchSizeValue, out var parsedBatchSize))
                {
                    batchSize = parsedBatchSize;
                }
                else
                {
                    throw new ConfigurationErrorsException($"Valor inválido para '{batchSizeKey}': '{batchSizeValue}'. Esperado um número inteiro.");
                }
            }

            var batchIntervalKey = "CustomLogger:Batch:FlushIntervalMs";
            var batchIntervalValue = appSettings[batchIntervalKey];
            int? flushIntervalMs = null;
            if (!string.IsNullOrWhiteSpace(batchIntervalValue))
            {
                if (int.TryParse(batchIntervalValue, out var parsedFlushInterval))
                {
                    flushIntervalMs = parsedFlushInterval;
                }
                else
                {
                    throw new ConfigurationErrorsException($"Valor inválido para '{batchIntervalKey}': '{batchIntervalValue}'. Esperado um número inteiro.");
                }
            }

            if (batchSize.HasValue || flushIntervalMs.HasValue)
            {
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

