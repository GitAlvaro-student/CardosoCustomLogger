using System;
using System.Collections.Generic;
using System.Text;

namespace CustomLogger.OpenTelemetry
{
    /// <summary>
    /// Opções de configuração para integração MyLogger com OpenTelemetry.
    /// Suporta binding via IConfiguration e override fluente.
    /// </summary>
    public sealed class CustomLoggerOpenTelemetryOptions
    {
        /// <summary>
        /// Indica se a integração OpenTelemetry está habilitada.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Nome do exporter a ser utilizado ("Otlp", "Console", "None").
        /// </summary>
        public string Exporter { get; set; }

        /// <summary>
        /// Configurações de instrumentações.
        /// </summary>
        public CustomLoggerInstrumentationOptions Instrumentations { get; set; } = new CustomLoggerInstrumentationOptions();

        /// <summary>
        /// Habilita instrumentação ASP.NET Core (fluent API).
        /// </summary>
        public CustomLoggerOpenTelemetryOptions UseAspNetCoreInstrumentation()
        {
            Instrumentations.AspNetCore = true;
            return this;
        }

        /// <summary>
        /// Habilita instrumentação HttpClient (fluent API).
        /// </summary>
        public CustomLoggerOpenTelemetryOptions UseHttpClientInstrumentation()
        {
            Instrumentations.HttpClient = true;
            return this;
        }

        /// <summary>
        /// Habilita instrumentação Oracle (fluent API).
        /// </summary>
        public CustomLoggerOpenTelemetryOptions UseOracleInstrumentation()
        {
            Instrumentations.Oracle = true;
            return this;
        }

        /// <summary>
        /// Configura exporter OTLP (fluent API).
        /// </summary>
        public CustomLoggerOpenTelemetryOptions UseOtlpExporter()
        {
            Exporter = "Otlp";
            return this;
        }

        /// <summary>
        /// Configura exporter Console (fluent API).
        /// </summary>
        public CustomLoggerOpenTelemetryOptions UseConsoleExporter()
        {
            Exporter = "Console";
            return this;
        }
    }

    /// <summary>
    /// Opções de instrumentações disponíveis.
    /// </summary>
    public sealed class CustomLoggerInstrumentationOptions
    {
        /// <summary>
        /// Habilita instrumentação ASP.NET Core.
        /// </summary>
        public bool AspNetCore { get; set; }

        /// <summary>
        /// Habilita instrumentação HttpClient.
        /// </summary>
        public bool HttpClient { get; set; }

        /// <summary>
        /// Habilita instrumentação Oracle.
        /// </summary>
        public bool Oracle { get; set; }
    }
}
