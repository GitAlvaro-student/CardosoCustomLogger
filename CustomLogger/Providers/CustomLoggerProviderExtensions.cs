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

        // ✅ Para Web API .NET Framework 4.7.2
        public static ILoggingBuilder AddCustomLogging(
            this ILoggingBuilder builder,
            Action<CustomProviderOptions> configure)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            var options = new CustomProviderOptions();
            configure?.Invoke(options);

            var provider = new CustomLoggerProviderBuilder()
                .WithOptions(options)
                .AddConsoleSink()
                .AddFileSink("App_Data/logs/app.log")  // ← Path padrão
                .Build();

            builder.AddProvider(provider);
            return builder;
        }

        // ✅ Para aplicações sem ILoggingBuilder
        public static ILoggerFactory CreateCustomLoggerFactory(
            Action<CustomProviderOptions> configure = null)
        {
            var options = new CustomProviderOptions
            {
                MinimumLogLevel = LogLevel.Information,
                UseGlobalBuffer = true,
                MaxBufferSize = 50
            };

            configure?.Invoke(options);

            var provider = new CustomLoggerProviderBuilder()
                .WithOptions(options)
                .AddConsoleSink()
                .AddFileSink("logs/app.log")
                .Build();

            return LoggerFactory.Create(builder =>
            {
                builder.AddProvider(provider);
            });
        }
    }
}
