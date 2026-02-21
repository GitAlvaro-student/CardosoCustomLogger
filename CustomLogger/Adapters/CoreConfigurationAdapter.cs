using CustomLogger.Configurations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;

namespace CustomLogger.Adapters
{
    /// <summary>
    /// Adaptador para converter IConfiguration em LoggingOptions.
    /// Usado em aplicações .NET Core/.NET 5+.
    /// </summary>
    public sealed class CoreConfigurationAdapter
    {
        /// <summary>
        /// Cria LoggingOptions a partir de IConfiguration.
        /// 
        /// REFATORADO: CC = 1 (ANTES: CC = 60)
        /// Complexidade delegada para métodos privados coesos.
        /// </summary>
        public LoggingOptions CreateFromConfiguration(IConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            var section = configuration.GetSection("CustomLogger");

            // Parsing delegado para métodos privados (CC: +0 cada)
            var (minimumLogLevel, serviceName, environment) = ParsePrimitiveOptions(section);
            var bufferOptions = ParseBufferOptions(section);
            var sinkOptions = ParseSinkOptions(section);
            var batchOptions = ParseBatchOptions(section);

            return new LoggingOptions(
                minimumLogLevel,
                serviceName,
                environment,
                bufferOptions,
                batchOptions,
                sinkOptions
            );
        }

        #region Primitive Options Parsing

        /// <summary>
        /// Extrai configurações primitivas do nível raiz.
        /// CC: ~3-5
        /// </summary>
        private (LogLevel? minimumLogLevel, string serviceName, string environment) ParsePrimitiveOptions(
            IConfigurationSection section)
        {
            var minimumLogLevel = ParseEnum<LogLevel>(section, "MinimumLogLevel");
            var serviceName = ParseString(section, "ServiceName");
            var environment = ParseString(section, "Environment");

            return (minimumLogLevel, serviceName, environment);
        }

        #endregion

        #region Buffer Options Parsing

        /// <summary>
        /// Constrói BufferOptions a partir da seção "Buffer".
        /// CC: ~4-7
        /// </summary>
        private BufferOptions ParseBufferOptions(IConfigurationSection section)
        {
            var bufferSection = section.GetSection("Buffer");
            if (!bufferSection.Exists())
                return null;

            var bufferEnabled = ParseBool(bufferSection, "Enabled");
            var maxSize = ParseInt(bufferSection, "MaxSize");

            return new BufferOptions(bufferEnabled, maxSize);
        }

        #endregion

        #region Sink Options Parsing

        /// <summary>
        /// Constrói SinkOptions agregando todos os sinks configurados.
        /// CC: ~2
        /// </summary>
        private SinkOptions ParseSinkOptions(IConfigurationSection section)
        {
            var sinksSection = section.GetSection("Sinks");
            if (!sinksSection.Exists())
                return null;

            var consoleOptions = ParseConsoleSinkOptions(sinksSection);
            var fileOptions = ParseFileSinkOptions(sinksSection);
            var blobOptions = ParseBlobStorageSinkOptions(sinksSection);
            var dynatraceOptions = ParseDynatraceSinkOptions(sinksSection);

            return new SinkOptions(
                consoleOptions,
                fileOptions,
                blobOptions,
                dynatraceOptions
            );
        }

        /// <summary>
        /// Constrói ConsoleSinkOptions a partir da seção "Sinks:Console".
        /// CC: ~3-6
        /// </summary>
        private ConsoleSinkOptions ParseConsoleSinkOptions(IConfigurationSection sinksSection)
        {
            var consoleSection = sinksSection.GetSection("Console");
            if (!consoleSection.Exists())
                return null;

            var consoleEnabled = ParseBool(consoleSection, "Enabled");
            return new ConsoleSinkOptions(consoleEnabled);
        }

        /// <summary>
        /// Constrói FileSinkOptions a partir da seção "Sinks:File".
        /// CC: ~3-6
        /// </summary>
        private FileSinkOptions ParseFileSinkOptions(IConfigurationSection sinksSection)
        {
            var fileSection = sinksSection.GetSection("File");
            if (!fileSection.Exists())
                return null;

            var fileEnabled = ParseBool(fileSection, "Enabled");
            var filePath = ParseString(fileSection, "Path");

            return new FileSinkOptions(fileEnabled, filePath);
        }

        /// <summary>
        /// Constrói BlobStorageSinkOptions a partir da seção "Sinks:BlobStorage".
        /// CC: ~3-6
        /// </summary>
        private BlobStorageSinkOptions ParseBlobStorageSinkOptions(IConfigurationSection sinksSection)
        {
            var blobSection = sinksSection.GetSection("BlobStorage");
            if (!blobSection.Exists())
                return null;

            var blobEnabled = ParseBool(blobSection, "Enabled");
            var connectionString = ParseString(blobSection, "ConnectionString");
            var containerName = ParseString(blobSection, "ContainerName");

            return new BlobStorageSinkOptions(
                blobEnabled,
                connectionString,
                containerName
            );
        }

        /// <summary>
        /// Constrói DynatraceSinkOptions a partir da seção "Sinks:Dynatrace".
        /// CC: ~4-10
        /// </summary>
        private DynatraceSinkOptions ParseDynatraceSinkOptions(IConfigurationSection sinksSection)
        {
            var dynatraceSection = sinksSection.GetSection("Dynatrace");
            if (!dynatraceSection.Exists())
                return null;

            var dynatraceEnabled = ParseBool(dynatraceSection, "Enabled");
            var endpoint = ParseString(dynatraceSection, "Endpoint");
            var apiToken = ParseString(dynatraceSection, "ApiToken");
            var timeoutSeconds = ParseInt(dynatraceSection, "TimeoutSeconds");

            return new DynatraceSinkOptions(
                dynatraceEnabled,
                endpoint,
                apiToken,
                timeoutSeconds
            );
        }

        #endregion

        #region Batch Options Parsing

        /// <summary>
        /// Constrói BatchOptions a partir da seção "Batch".
        /// CC: ~4-7
        /// </summary>
        private BatchOptions ParseBatchOptions(IConfigurationSection section)
        {
            var batchSection = section.GetSection("Batch");
            if (!batchSection.Exists())
                return null;

            var batchSize = ParseInt(batchSection, "BatchSize");
            var flushIntervalMs = ParseInt(batchSection, "FlushIntervalMs");

            return new BatchOptions(batchSize, flushIntervalMs);
        }

        #endregion

        #region Generic Parsing Helpers

        /// <summary>
        /// Parse string opcional de uma seção de configuração.
        /// Retorna null se valor não existir ou for vazio.
        /// CC: ~1
        /// </summary>
        private string ParseString(IConfigurationSection section, string key)
        {
            var value = section[key];
            if (!string.IsNullOrWhiteSpace(value))
                return value;

            return null;
        }

        /// <summary>
        /// Parse bool opcional de uma seção de configuração.
        /// Retorna null se valor não existir, for vazio ou inválido.
        /// CC: ~2
        /// </summary>
        private bool? ParseBool(IConfigurationSection section, string key)
        {
            var value = section[key];
            if (!string.IsNullOrWhiteSpace(value) && bool.TryParse(value, out var parsed))
                return parsed;

            return null;
        }

        /// <summary>
        /// Parse int opcional de uma seção de configuração.
        /// Retorna null se valor não existir, for vazio ou inválido.
        /// CC: ~2
        /// </summary>
        private int? ParseInt(IConfigurationSection section, string key)
        {
            var value = section[key];
            if (!string.IsNullOrWhiteSpace(value) && int.TryParse(value, out var parsed))
                return parsed;

            return null;
        }

        /// <summary>
        /// Parse enum opcional de uma seção de configuração.
        /// Retorna null se valor não existir, for vazio ou inválido.
        /// CC: ~2
        /// </summary>
        private T? ParseEnum<T>(IConfigurationSection section, string key) where T : struct, Enum
        {
            var value = section[key];
            if (!string.IsNullOrWhiteSpace(value) && Enum.TryParse<T>(value, ignoreCase: true, out var parsed))
                return parsed;

            return null;
        }

        #endregion
    }
}