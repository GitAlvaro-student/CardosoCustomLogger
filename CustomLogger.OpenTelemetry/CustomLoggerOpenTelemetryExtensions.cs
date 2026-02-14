using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Trace;
using System;
using System.Collections.Generic;
using System.Text;

namespace CustomLogger.OpenTelemetry
{
    /// <summary>
    /// Métodos de extensão para registrar CustomLogger OpenTelemetry em IServiceCollection.
    /// </summary>
    public static class CustomLoggerOpenTelemetryExtensions
    {
        private const string ConfigurationSection = "CustomLogger:OpenTelemetry";

        /// <summary>
        /// Adiciona integração CustomLogger com OpenTelemetry usando configuração de appsettings.json.
        /// </summary>
        /// <param name="services">Service collection.</param>
        /// <param name="configuration">Configuration root.</param>
        /// <returns>Service collection para chamadas fluentes.</returns>
        public static IServiceCollection AddCustomLoggerOpenTelemetry(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            return AddCustomLoggerOpenTelemetry(services, configuration, null);
        }

        /// <summary>
        /// Adiciona integração CustomLogger com OpenTelemetry usando configuração de appsettings.json
        /// com possibilidade de override via API fluente.
        /// </summary>
        /// <param name="services">Service collection.</param>
        /// <param name="configuration">Configuration root.</param>
        /// <param name="configureOptions">Ação para override de configuração via API fluente.</param>
        /// <returns>Service collection para chamadas fluentes.</returns>
        public static IServiceCollection AddCustomLoggerOpenTelemetry(
            this IServiceCollection services,
            IConfiguration configuration,
            Action<CustomLoggerOpenTelemetryOptions> configureOptions)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            // 1. Bind da configuração
            var options = new CustomLoggerOpenTelemetryOptions();
            var section = configuration.GetSection(ConfigurationSection);
            section.Bind(options);

            // 2. Aplicar override fluente (se fornecido)
            configureOptions?.Invoke(options);

            // 3. Se desabilitado, não registrar OpenTelemetry
            if (!options.Enabled)
                return services;

            // 4. Registrar OpenTelemetry com instrumentações condicionais
            services.AddOpenTelemetry()
                .WithTracing(builder =>
                {
                    // Adiciona source do CustomLogger
                    builder.AddSource(LoggerActivitySource.Source.Name);

                    // Instrumentações condicionais
                    if (options.Instrumentations.AspNetCore)
                    {
                        builder.AddAspNetCoreInstrumentation();
                    }

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
                });

            return services;
        }
    }
}
