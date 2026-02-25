using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Trace;
using System;

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
        /// 
        /// REFATORADO: CC = 1 (ANTES: CC = 10)
        /// Complexidade delegada para métodos privados coesos.
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
            // Delegação para métodos privados (CC: +0 cada)
            ValidateArguments(services, configuration);

            var options = BuildOptions(configuration, configureOptions);

            // Guard clause: Skip se OpenTelemetry desabilitado
            if (!options.Enabled)                                  // CC: +1
                return services;

            // Registrar OpenTelemetry com configuração delegada
            services.AddOpenTelemetry()
                .WithTracing(builder =>
                {
                    // Adiciona source do CustomLogger
                    builder.AddSource(LoggerActivitySource.Source.Name);

                    // Configuração delegada para métodos privados
                    ConfigureInstrumentations(builder, options.Instrumentations);
                    ConfigureExporter(builder, options.Exporter);
                });

            return services;
        }

        #region Validation

        /// <summary>
        /// Valida argumentos obrigatórios.
        /// CC: ~2
        /// </summary>
        private static void ValidateArguments(
            IServiceCollection services,
            IConfiguration configuration)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
        }

        #endregion

        #region Options Building

        /// <summary>
        /// Constrói opções de OpenTelemetry a partir de IConfiguration.
        /// Aplica override fluente se fornecido.
        /// CC: ~1
        /// </summary>
        private static CustomLoggerOpenTelemetryOptions BuildOptions(
            IConfiguration configuration,
            Action<CustomLoggerOpenTelemetryOptions> configureOptions)
        {
            // 1. Bind da configuração
            var options = new CustomLoggerOpenTelemetryOptions();
            var section = configuration.GetSection(ConfigurationSection);
            section.Bind(options);

            // 2. Aplicar override fluente (se fornecido)
            configureOptions?.Invoke(options);

            return options;
        }

        #endregion

        #region Instrumentations Configuration

        /// <summary>
        /// Configura instrumentações do OpenTelemetry baseado nas opções.
        /// CC: ~2
        /// </summary>
        private static void ConfigureInstrumentations(
            TracerProviderBuilder builder,
            CustomLoggerInstrumentationOptions instrumentations)
        {
            // Instrumentação AspNetCore condicional
            if (instrumentations.AspNetCore)
                builder.AddAspNetCoreInstrumentation();

            // Instrumentação HttpClient condicional
            if (instrumentations.HttpClient)
                builder.AddHttpClientInstrumentation();

            // Instrumentação Oracle (comentada - exemplo para futura implementação)
            //if (instrumentations.Oracle)
            //    builder.AddOracleDataProviderInstrumentation();
        }

        #endregion

        #region Exporter Configuration

        /// <summary>
        /// Configura exporter do OpenTelemetry baseado na string de configuração.
        /// CC: ~4
        /// </summary>
        private static void ConfigureExporter(
            TracerProviderBuilder builder,
            string exporter)
        {
            // Guard clause: Skip se exporter não configurado
            if (string.IsNullOrWhiteSpace(exporter))
                return;

            // Adicionar exporter baseado em configuração
            switch (exporter.ToLowerInvariant())
            {
                case "otlp":
                    builder.AddOtlpExporter();
                    break;

                case "console":
                    builder.AddConsoleExporter();
                    break;

                    // Outros exporters podem ser adicionados aqui:
                    // case "jaeger":
                    //     builder.AddJaegerExporter();
                    //     break;
                    // case "zipkin":
                    //     builder.AddZipkinExporter();
                    //     break;
            }
        }

        #endregion
    }
}