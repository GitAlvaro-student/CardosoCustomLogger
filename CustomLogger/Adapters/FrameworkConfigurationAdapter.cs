using CustomLogger.Configurations;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;

namespace CustomLogger.Adapters
{
    public sealed class FrameworkConfigurationAdapter
    {
        public LoggingOptions CreateFromAppSettings()
        {
            var appSettings = ConfigurationManager.AppSettings;

            // MinimumLogLevel
            var minimumLogLevel = ParseNullableLogLevel(appSettings, "CustomLogger:MinimumLogLevel");

            // ServiceName (required)
            var serviceName = ReadRequiredString(appSettings, "CustomLogger:ServiceName", "ServiceName");

            // Environment (required)
            var environment = ReadRequiredString(appSettings, "CustomLogger:Environment", "Environment");

            // BufferOptions
            var bufferOptions = BuildBufferOptions(appSettings);

            // SinkOptions (console, file, blob, dynatrace)
            var consoleOptions = BuildConsoleOptions(appSettings);
            var fileOptions = BuildFileOptions(appSettings);
            var blobOptions = BuildBlobOptions(appSettings);
            var dynatraceOptions = BuildDynatraceOptions(appSettings);

            SinkOptions sinkOptions = null;
            if (consoleOptions != null || fileOptions != null || blobOptions != null || dynatraceOptions != null)
            {
                sinkOptions = new SinkOptions(
                    consoleOptions,
                    fileOptions,
                    blobOptions,
                    dynatraceOptions
                );
            }

            // BatchOptions
            var batchOptions = BuildBatchOptions(appSettings);

            return new LoggingOptions(
                minimumLogLevel,
                serviceName,
                environment,
                bufferOptions,
                batchOptions,
                sinkOptions
            );
        }

        #region Helpers: parsing / validation / object construction
        private LogLevel? ParseNullableLogLevel(System.Collections.Specialized.NameValueCollection appSettings, string key)
        {
            var value = appSettings[key];
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (Enum.TryParse<LogLevel>(value, ignoreCase: true, out var parsed))
            {
                return parsed;
            }

            throw new ConfigurationErrorsException($"Valor inválido para '{key}': '{value}'. Esperado um valor válido de LogLevel.");
        }

        private string ReadRequiredString(System.Collections.Specialized.NameValueCollection appSettings, string key, string expectedName)
        {
            var value = appSettings[key];
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            throw new ConfigurationErrorsException($"Valor ausente para '{key}': '{value}'. Esperado um valor para {expectedName}.");
        }

        private bool? ParseNullableBool(System.Collections.Specialized.NameValueCollection appSettings, string key, string errorTemplate)
        {
            var value = appSettings[key];
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (bool.TryParse(value, out var parsed))
            {
                return parsed;
            }

            // errorTemplate must contain placeholders {0} for key and {1} for value
            throw new ConfigurationErrorsException(string.Format(errorTemplate, key, value));
        }

        private int? ParseNullableInt(System.Collections.Specialized.NameValueCollection appSettings, string key, string errorTemplate)
        {
            var value = appSettings[key];
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (int.TryParse(value, out var parsed))
            {
                return parsed;
            }

            // errorTemplate must contain placeholders {0} for key and {1} for value
            throw new ConfigurationErrorsException(string.Format(errorTemplate, key, value));
        }

        private BufferOptions BuildBufferOptions(System.Collections.Specialized.NameValueCollection appSettings)
        {
            var enabledKey = "CustomLogger:Buffer:Enabled";
            var maxSizeKey = "CustomLogger:Buffer:MaxSize";

            var enabled = ParseNullableBool(appSettings, enabledKey, "Valor inválido para '{0}': '{1}'. Esperado 'true' ou 'false'.");
            var maxSize = ParseNullableInt(appSettings, maxSizeKey, "Valor inválido para '{0}': '{1}'. Esperado um número inteiro.");

            if (enabled.HasValue || maxSize.HasValue)
            {
                return new BufferOptions(enabled, maxSize);
            }

            return null;
        }

        private ConsoleSinkOptions BuildConsoleOptions(System.Collections.Specialized.NameValueCollection appSettings)
        {
            var key = "CustomLogger:Sinks:Console:Enabled";
            var raw = appSettings[key];
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            var parsed = ParseNullableBool(appSettings, key, "Valor inválido para '{0}': '{1}'. Esperado 'true' ou 'false'.");
            // original code created ConsoleSinkOptions when the key exists (non-empty)
            return new ConsoleSinkOptions(parsed);
        }

        private FileSinkOptions BuildFileOptions(System.Collections.Specialized.NameValueCollection appSettings)
        {
            var enabledKey = "CustomLogger:Sinks:File:Enabled";
            var pathKey = "CustomLogger:Sinks:File:Path";

            var enabled = ParseNullableBool(appSettings, enabledKey, "Valor inválido para '{0}': '{1}'. Esperado 'true' ou 'false'.");
            var path = appSettings[pathKey];

            if (enabled.HasValue || !string.IsNullOrWhiteSpace(path))
            {
                return new FileSinkOptions(enabled, path);
            }

            return null;
        }

        private BlobStorageSinkOptions BuildBlobOptions(System.Collections.Specialized.NameValueCollection appSettings)
        {
            var enabledKey = "CustomLogger:Sinks:BlobStorage:Enabled";
            var connKey = "CustomLogger:Sinks:BlobStorage:ConnectionString";
            var containerKey = "CustomLogger:Sinks:BlobStorage:ContainerName";

            var enabled = ParseNullableBool(appSettings, enabledKey, "Valor inválido para '{0}': '{1}'. Esperado 'true' ou 'false'.");
            var connectionString = appSettings[connKey];
            var containerName = appSettings[containerKey];

            if (enabled.HasValue || !string.IsNullOrWhiteSpace(connectionString) || !string.IsNullOrWhiteSpace(containerName))
            {
                return new BlobStorageSinkOptions(enabled, connectionString, containerName);
            }

            return null;
        }

        private DynatraceSinkOptions BuildDynatraceOptions(System.Collections.Specialized.NameValueCollection appSettings)
        {
            var enabledKey = "CustomLogger:Sinks:Dynatrace:Enabled";
            var endpointKey = "CustomLogger:Sinks:Dynatrace:Endpoint";
            var apiTokenKey = "CustomLogger:Sinks:Dynatrace:ApiToken";
            var timeoutKey = "CustomLogger:Sinks:Dynatrace:TimeoutSeconds";

            var enabled = ParseNullableBool(appSettings, enabledKey, "Valor inválido para '{0}': '{1}'.");
            var endpoint = appSettings[endpointKey];
            var apiToken = appSettings[apiTokenKey];
            var timeout = ParseNullableInt(appSettings, timeoutKey, "Valor inválido para '{0}': '{1}'.");

            if (enabled.HasValue || !string.IsNullOrWhiteSpace(endpoint) ||
                !string.IsNullOrWhiteSpace(apiToken) || timeout.HasValue)
            {
                return new DynatraceSinkOptions(
                    enabled,
                    endpoint,
                    apiToken,
                    timeout
                );
            }

            return null;
        }

        private BatchOptions BuildBatchOptions(System.Collections.Specialized.NameValueCollection appSettings)
        {
            var batchSizeKey = "CustomLogger:Batch:BatchSize";
            var batchIntervalKey = "CustomLogger:Batch:FlushIntervalMs";

            var batchSize = ParseNullableInt(appSettings, batchSizeKey, "Valor inválido para '{0}': '{1}'. Esperado um número inteiro.");
            var flushIntervalMs = ParseNullableInt(appSettings, batchIntervalKey, "Valor inválido para '{0}': '{1}'. Esperado um número inteiro.");

            if (batchSize.HasValue || flushIntervalMs.HasValue)
            {
                return new BatchOptions(batchSize, flushIntervalMs);
            }

            return null;
        }
        #endregion
    }
}

