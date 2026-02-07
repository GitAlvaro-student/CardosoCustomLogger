using CustomLogger.Abstractions;
using CustomLogger.Configurations;
using CustomLogger.Formatting;
using CustomLogger.Sinks;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace CustomLogger.Providers
{
    public sealed class CustomLoggerProviderBuilder
    {
        private readonly List<ILogSink> _sinks = new List<ILogSink>();
        private LoggingOptions _loggingOptions;
        private CustomProviderOptions _options;
        private bool _defaultsApplied = false;
        private bool _validated = false;

        public CustomLoggerProviderBuilder WithOptions(CustomProviderOptions options)
        {
            _options = options;
            return this;
        }

        // ✅ Método que aceita lambda para configuração
        public CustomLoggerProviderBuilder WithOptions(Action<CustomProviderOptions> configure)
        {
            _options = _options != null ? _options : new CustomProviderOptions();
            configure?.Invoke(_options);
            return this;
        }

        public CustomLoggerProviderBuilder WithLoggingOptions(LoggingOptions loggingOptions)
        {
            _loggingOptions = loggingOptions ?? throw new ArgumentNullException(nameof(loggingOptions));
            return this;
        }

        // Mantém os métodos de adição de sinks existentes
        public CustomLoggerProviderBuilder AddSinkWithDegradation(ILogSink sink)
        {
            var degradableSink = new DegradableLogSink(sink);
            _sinks.Add(degradableSink);
            return this;
        }

        public CustomLoggerProviderBuilder AddSinkWithoutDegradation(ILogSink sink)
        {
            _sinks.Add(sink);
            return this;
        }

        public CustomLoggerProviderBuilder AddSink(ILogSink sink)
        {
            if (sink != null)
                _sinks.Add(sink);
            return this;
        }

        public CustomLoggerProviderBuilder AddConsoleSink(ILogFormatter formatter = null)
        {
            _sinks.Add(new ConsoleLogSink(formatter ?? new JsonLogFormatter()));
            return this;
        }

        public CustomLoggerProviderBuilder AddFileSink(string path, ILogFormatter formatter = null)
        {
            _sinks.Add(new FileLogSink(path, formatter ?? new JsonLogFormatter()));
            return this;
        }

        public CustomLoggerProviderBuilder AddBlobSink(string connectionString, string container, ILogFormatter formatter = null)
        {
            _sinks.Add(new BlobStorageLogSink(connectionString, container, formatter ?? new JsonLogFormatter()));
            return this;
        }

        public CustomLoggerProvider Build()
        {
            if (_loggingOptions == null)
                throw new InvalidOperationException("LoggingOptions não configurado. Use WithLoggingOptions().");

            // 1. Aplica defaults finais do Core
            ApplyCoreDefaults();

            // 2. Executa validação de configuração
            ValidateConfiguration();

            // 3. Converte LoggingOptions para CustomProviderOptions
            var customProviderOptions = ConvertToCustomProviderOptions();

            // 4. Cria os sinks configurados via LoggingOptions
            CreateSinksFromOptions();

            if (_sinks.Count == 0)
                throw new InvalidOperationException("Nenhum sink configurado. Use métodos Add*Sink() ou configure sinks via LoggingOptions.");

            var compositeSink = new CompositeLogSink(_sinks);
            return new CustomLoggerProvider(customProviderOptions, compositeSink, _sinks);
        }

        private void ApplyCoreDefaults()
        {
            if (_defaultsApplied)
                return;

            // Aplica defaults do Core
            if (!_loggingOptions.MinimumLogLevel.HasValue)
            {
                _loggingOptions.MinimumLogLevel = LogLevel.Information;
            }

            if (_loggingOptions.BufferOptions == null)
            {
                _loggingOptions.BufferOptions = new BufferOptions
                {
                    Enabled = true,
                    MaxSize = 50,
                    FlushIntervalMs = 5000
                };
            }
            else
            {
                // Aplica defaults para propriedades individuais do buffer
                var buffer = _loggingOptions.BufferOptions;
                if (!buffer.Enabled.HasValue)
                    buffer.Enabled = true;
                if (!buffer.MaxSize.HasValue)
                    buffer.MaxSize = 50;
                if (!buffer.FlushIntervalMs.HasValue)
                    buffer.FlushIntervalMs = 5000;
            }

            // Aplica defaults para sinks
            if (_loggingOptions.SinkOptions != null)
            {
                var sinks = _loggingOptions.SinkOptions;

                if (sinks.Console != null && !sinks.Console.Enabled.HasValue)
                    sinks.Console.Enabled = true;

                if (sinks.File != null && !sinks.File.Enabled.HasValue)
                    sinks.File.Enabled = false;

                if (sinks.BlobStorage != null && !sinks.BlobStorage.Enabled.HasValue)
                    sinks.BlobStorage.Enabled = false;
            }

            _defaultsApplied = true;
        }

        private void ValidateConfiguration()
        {
            if (_validated)
                return;

            // Validação de configuração
            var buffer = _loggingOptions.BufferOptions;
            if (buffer != null && buffer.Enabled.HasValue && buffer.Enabled.Value)
            {
                if (!buffer.MaxSize.HasValue || buffer.MaxSize.Value <= 0)
                {
                    throw new InvalidOperationException(
                        "BufferOptions.MaxSize deve ser maior que zero quando BufferOptions.Enabled é true.");
                }

                if (!buffer.FlushIntervalMs.HasValue || buffer.FlushIntervalMs.Value <= 0)
                {
                    throw new InvalidOperationException(
                        "BufferOptions.FlushIntervalMs deve ser maior que zero quando BufferOptions.Enabled é true.");
                }
            }

            // Valida FileSink
            var fileSink = _loggingOptions.SinkOptions?.File;
            if (fileSink != null && fileSink.Enabled.HasValue && fileSink.Enabled.Value)
            {
                if (string.IsNullOrWhiteSpace(fileSink.Path))
                {
                    throw new InvalidOperationException(
                        "FileSinkOptions.Path não pode ser vazio quando FileSinkOptions.Enabled é true.");
                }
            }

            // Valida BlobStorageSink
            var blobSink = _loggingOptions.SinkOptions?.BlobStorage;
            if (blobSink != null && blobSink.Enabled.HasValue && blobSink.Enabled.Value)
            {
                if (string.IsNullOrWhiteSpace(blobSink.ConnectionString))
                {
                    throw new InvalidOperationException(
                        "BlobStorageSinkOptions.ConnectionString não pode ser vazio quando BlobStorageSinkOptions.Enabled é true.");
                }

                if (string.IsNullOrWhiteSpace(blobSink.ContainerName))
                {
                    throw new InvalidOperationException(
                        "BlobStorageSinkOptions.ContainerName não pode ser vazio quando BlobStorageSinkOptions.Enabled é true.");
                }
            }

            _validated = true;
        }

        private CustomProviderOptions ConvertToCustomProviderOptions()
        {
            var options = new CustomProviderOptions
            {
                MinimumLogLevel = _loggingOptions.MinimumLogLevel.Value
            };

            if (_loggingOptions.BufferOptions != null && _loggingOptions.BufferOptions.Enabled.Value)
            {
                options.UseGlobalBuffer = true;
                options.MaxBufferSize = _loggingOptions.BufferOptions.MaxSize.Value;
                options.BatchOptions = new BatchOptions
                {
                    BatchSize = 50,
                    FlushInterval = TimeSpan.FromMilliseconds(_loggingOptions.BufferOptions.FlushIntervalMs.Value)
                };
            }

            return options;
        }

        private void CreateSinksFromOptions()
        {
            var sinks = _loggingOptions.SinkOptions;
            if (sinks == null)
                return;

            // Cria Console sink se configurado
            if (sinks.Console != null && sinks.Console.Enabled.HasValue && sinks.Console.Enabled.Value)
            {
                if (!SinkExists<ConsoleLogSink>())
                {
                    AddConsoleSink();
                }
            }

            // Cria File sink se configurado
            if (sinks.File != null && sinks.File.Enabled.HasValue && sinks.File.Enabled.Value)
            {
                if (!string.IsNullOrWhiteSpace(sinks.File.Path))
                {
                    AddFileSink(sinks.File.Path);
                }
            }

            // Cria BlobStorage sink se configurado
            if (sinks.BlobStorage != null && sinks.BlobStorage.Enabled.HasValue && sinks.BlobStorage.Enabled.Value)
            {
                if (!string.IsNullOrWhiteSpace(sinks.BlobStorage.ConnectionString) &&
                    !string.IsNullOrWhiteSpace(sinks.BlobStorage.ContainerName))
                {
                    AddBlobSink(sinks.BlobStorage.ConnectionString, sinks.BlobStorage.ContainerName);
                }
            }
        }

        private bool SinkExists<T>() where T : ILogSink
        {
            foreach (var sink in _sinks)
            {
                if (sink is T)
                    return true;
            }
            return false;
        }
    }
}
