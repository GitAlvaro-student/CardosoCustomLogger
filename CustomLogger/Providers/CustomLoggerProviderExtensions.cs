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

        public static ILoggingBuilder AddCustomLogging(
        this ILoggingBuilder builder,
        IConfiguration configuration)
        {
            var section = configuration.GetSection("CustomLogger");

            var config = section.Get<LoggingOptions>();
            if (config is null)
                return builder;

            builder.AddCustomLogging(
                options => ApplyOptions(options, config),
                sinks => ApplySinks(sinks, config, configuration)
            );

            return builder;
        }

        private static void ApplyOptions(
        CustomProviderOptions options,
        LoggingOptions config)
        {
            if (Enum.TryParse<LogLevel>(
                config.MinimumLogLevel.ToString(),
                ignoreCase: true,
                out var level))
            {
                options.MinimumLogLevel = level;
            }

            if (config.BufferOptions != null && (bool)config.BufferOptions.Enabled)
            {
                options.UseGlobalBuffer = true;
                options.MaxBufferSize = config.BufferOptions.MaxSize;
                options.BatchOptions = new BatchOptions
                {
                    BatchSize = 30,
                    FlushInterval = TimeSpan.FromSeconds(5)
                };
            }
        }

        private static void ApplySinks(
            CustomLoggerProviderBuilder builder,
            LoggingOptions config,
            IConfiguration rootConfig)
        {
            var sinks = config.SinkOptions;
            if (sinks is null)
                return;

            if (sinks.Console?.Enabled == true)
            {
                builder.AddConsoleSink();
            }

            if (sinks.File?.Enabled == true &&
                !string.IsNullOrWhiteSpace(sinks.File.Path))
            {
                builder.AddFileSink(sinks.File.Path);
            }

            if (sinks.BlobStorage?.Enabled == true && (
                    !string.IsNullOrWhiteSpace(sinks.BlobStorage.ConnectionString) && 
                    !string.IsNullOrWhiteSpace(sinks.BlobStorage.ContainerName)))
            {
                builder.AddBlobSink(sinks.BlobStorage.ConnectionString, sinks.BlobStorage.ContainerName);
            }


        }
    }
}
