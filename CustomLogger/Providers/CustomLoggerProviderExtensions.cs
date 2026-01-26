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
            Action<CustomProviderOptions> configure)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            var options = new CustomProviderOptions();
            configure?.Invoke(options);

            var configuration = new CustomProviderConfiguration(options);

            builder.AddProvider(new CustomLoggerProvider(configuration));

            return builder;
        }
    }
}
