using CustomLogger.Adapters;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace CustomLogger.Providers
{
    public static class FrameworkCustomLoggerExtensions
    {
        /// <summary>
        /// Adiciona o Custom Logger ao pipeline de logging usando appSettings.
        /// Para .NET Framework.
        /// </summary>
        public static ILoggingBuilder AddCustomLogging(
            this ILoggingBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            // 1. Cria o adapter específico para .NET Framework
            var adapter = new NetFrameworkConfigurationAdapter();

            // 2. Converte appSettings em LoggingOptions
            var loggingOptions = adapter.CreateFromAppSettings();

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
