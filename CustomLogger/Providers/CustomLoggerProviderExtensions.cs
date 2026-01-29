using CustomLogger.Configurations;
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
        /// Adiciona o Custom Logger ao pipeline de logging.
        /// </summary>
        public static ILoggingBuilder AddCustomLogging(
            this ILoggingBuilder builder,
            Action<CustomProviderOptions> configureOptions,
            Action<CustomLoggerProviderBuilder> configureSinks)
        {
            var options = new CustomProviderOptions();
            configureOptions?.Invoke(options);

            var providerBuilder = new CustomLoggerProviderBuilder()
                .WithOptions(options);

            configureSinks?.Invoke(providerBuilder);

            builder.AddProvider(providerBuilder.Build());
            return builder;
        }
    }
}
