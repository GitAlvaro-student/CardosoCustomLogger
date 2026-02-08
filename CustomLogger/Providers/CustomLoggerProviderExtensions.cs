using CustomLogger.Adapters;
using CustomLogger.Configurations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace CustomLogger.Providers
{
    /// <summary>
    /// Extensões para registrar o CustomLoggerProvider
    /// no pipeline de logging do .NET.
    /// </summary>
    public static class CustomLoggerProviderExtensions
    {
        /// <summary>
        /// Adiciona o Custom Logger ao pipeline de logging usando IConfiguration.
        /// Para .NET 8.
        /// </summary>
        public static ILoggingBuilder AddCustomLogging(
            this ILoggingBuilder builder,
            IConfiguration configuration)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            // 1. Cria o adapter específico para .NET 8
            var adapter = new CoreConfigurationAdapter();

            // 2. Converte IConfiguration em LoggingOptions
            var loggingOptions = adapter.CreateFromConfiguration(configuration);

            // 3. Passa LoggingOptions para o Builder
            var provider = new CustomLoggerProviderBuilder()
                .WithLoggingOptions(loggingOptions)
                .BuildApplication();

            // 4. Registra o Provider
            builder.AddProvider(provider);

            return builder;
        }
    }
}
