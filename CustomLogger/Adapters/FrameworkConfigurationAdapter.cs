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

            // ServiceName
            string serviceName = null;
            var servNameKey = "CustomLogger:ServiceName";
            var servNameValue = appSettings[servNameKey];
            if (!string.IsNullOrWhiteSpace(servNameValue))
            {
                serviceName = servNameValue;
            }
            else throw new ConfigurationErrorsException($"Valor ausente para '{servNameKey}': '{servNameValue}'. Esperado um valor para ServiceName.");

            // Environment
            string environment = null;
            var envKey = "CustomLogger:Environment";
            var envValue = appSettings[envKey];
            if (!string.IsNullOrWhiteSpace(envValue))
            {
                environment = envValue;
            }
            else throw new ConfigurationErrorsException($"Valor ausente para '{envKey}': '{envValue}'. Esperado um valor para Environment.");

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

            // Dynatrace
            DynatraceSinkOptions dynatraceOptions = null;

            var dynatraceEnabledKey = "CustomLogger:Sinks:Dynatrace:Enabled";
            var dynatraceEnabledValue = appSettings[dynatraceEnabledKey];
            bool? dynatraceEnabled = null;
            if (!string.IsNullOrWhiteSpace(dynatraceEnabledValue))
            {
                if (bool.TryParse(dynatraceEnabledValue, out var parsedDynatraceEnabled))
                {
                    dynatraceEnabled = parsedDynatraceEnabled;
                }
                else
                {
                    throw new ConfigurationErrorsException($"Valor inválido para '{dynatraceEnabledKey}': '{dynatraceEnabledValue}'.");
                }
            }

            var endpointKey = "CustomLogger:Sinks:Dynatrace:Endpoint";
            var endpoint = appSettings[endpointKey];

            var apiTokenKey = "CustomLogger:Sinks:Dynatrace:ApiToken";
            var apiToken = appSettings[apiTokenKey];

            var timeoutKey = "CustomLogger:Sinks:Dynatrace:TimeoutSeconds";
            var timeoutValue = appSettings[timeoutKey];
            int? timeoutSeconds = null;
            if (!string.IsNullOrWhiteSpace(timeoutValue))
            {
                if (int.TryParse(timeoutValue, out var parsedTimeout))
                {
                    timeoutSeconds = parsedTimeout;
                }
                else
                {
                    throw new ConfigurationErrorsException($"Valor inválido para '{timeoutKey}': '{timeoutValue}'.");
                }
            }

            if (dynatraceEnabled.HasValue || !string.IsNullOrWhiteSpace(endpoint) ||
                !string.IsNullOrWhiteSpace(apiToken) || timeoutSeconds.HasValue)
            {
                dynatraceOptions = new DynatraceSinkOptions(
                    dynatraceEnabled,
                    endpoint,
                    apiToken,
                    timeoutSeconds
                );
            }

            if (consoleOptions != null || fileOptions != null ||
                blobOptions != null || dynatraceOptions != null) // Alterado
            {
                sinkOptions = new SinkOptions(
                    consoleOptions,
                    fileOptions,
                    blobOptions,
                    dynatraceOptions // NOVO
                );
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
                    serviceName,
                    environment,
                    bufferOptions,
                    batchOptions,
                    sinkOptions
                );
        }
    }
}

