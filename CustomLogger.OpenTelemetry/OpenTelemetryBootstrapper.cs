using OpenTelemetry;
using OpenTelemetry.Trace;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
using System.Threading;

namespace CustomLogger.OpenTelemetry
{
    /// <summary>
    /// Bootstrapper para inicialização manual do OpenTelemetry em .NET Framework.
    /// Thread-safe e idempotente.
    /// </summary>
    public static class OpenTelemetryBootstrapper
    {
        private static TracerProvider _tracerProvider;
        private static readonly object _lock = new object();
        private static int _initialized = 0;

        /// <summary>
        /// Inicializa OpenTelemetry com as opções fornecidas.
        /// Thread-safe e idempotente (não inicializa duas vezes).
        /// </summary>
        /// <param name="options">Opções de configuração.</param>
        public static void Initialize(CustomLoggerOpenTelemetryOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            if (!options.Enabled)
                return;

            // Verifica se já foi inicializado (thread-safe)
            if (Interlocked.CompareExchange(ref _initialized, 1, 0) != 0)
                return;

            lock (_lock)
            {
                if (_tracerProvider != null)
                    return;

                var builder = Sdk.CreateTracerProviderBuilder()
                    .AddSource(LoggerActivitySource.Source.Name);

                // Instrumentações condicionais
                if (options.Instrumentations.HttpClient)
                {
                    builder.AddHttpClientInstrumentation();
                }

                //if (options.Instrumentations.Oracle)
                //{
                //    builder.AddOracleDataProviderInstrumentation();
                //}

                // Exporter condicional
                if (!string.IsNullOrWhiteSpace(options.Exporter))
                {
                    switch (options.Exporter.ToLowerInvariant())
                    {
                        case "otlp":
                            builder.AddOtlpExporter();
                            break;
                        case "console":
                            builder.AddConsoleExporter();
                            break;
                    }
                }

                _tracerProvider = builder.Build();
            }
        }

        /// <summary>
        /// Inicializa OpenTelemetry lendo configuração de appSettings/web.config.
        /// </summary>
        public static void InitializeFromConfig()
        {
            var options = ReadOptionsFromConfig();
            Initialize(options);
        }

        /// <summary>
        /// Lê opções de configuração de appSettings (web.config ou app.config).
        /// </summary>
        private static CustomLoggerOpenTelemetryOptions ReadOptionsFromConfig()
        {
            var options = new CustomLoggerOpenTelemetryOptions
            {
                Enabled = ReadBoolSetting("CustomLogger:OpenTelemetry:Enabled", true),
                Exporter = ConfigurationManager.AppSettings["CustomLogger:OpenTelemetry:Exporter"],
                Instrumentations = new CustomLoggerInstrumentationOptions
                {
                    AspNetCore = ReadBoolSetting("CustomLogger:OpenTelemetry:Instrumentations:AspNetCore", false),
                    HttpClient = ReadBoolSetting("CustomLogger:OpenTelemetry:Instrumentations:HttpClient", false),
                    Oracle = ReadBoolSetting("CustomLogger:OpenTelemetry:Instrumentations:Oracle", false)
                }
            };

            return options;
        }

        private static bool ReadBoolSetting(string key, bool defaultValue)
        {
            var value = ConfigurationManager.AppSettings[key];
            if (string.IsNullOrWhiteSpace(value))
                return defaultValue;

            return bool.TryParse(value, out var result) ? result : defaultValue;
        }

        /// <summary>
        /// Libera recursos do TracerProvider.
        /// Deve ser chamado no Application_End (opcional).
        /// </summary>
        public static void Shutdown()
        {
            lock (_lock)
            {
                _tracerProvider?.Dispose();
                _tracerProvider = null;
                _initialized = 0;
            }
        }
    }
}
