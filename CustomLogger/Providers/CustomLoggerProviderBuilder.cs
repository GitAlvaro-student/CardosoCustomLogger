using CustomLogger.Abstractions;
using CustomLogger.Configurations;
using CustomLogger.Formatting;
using CustomLogger.Sinks;
using System;
using System.Collections.Generic;
using System.Text;

namespace CustomLogger.Providers
{
    public sealed class CustomLoggerProviderBuilder
    {
        private readonly List<ILogSink> _sinks = new List<ILogSink>();
        private CustomProviderOptions _options;

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

        // CustomLoggerProviderBuilder.cs
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
            if (_options == null)
                throw new InvalidOperationException("Options não configurado. Use WithOptions().");

            if (_sinks.Count == 0)
                throw new InvalidOperationException("Nenhum sink configurado. Use Add*Sink().");

            var compositeSink = new CompositeLogSink(_sinks);
            return new CustomLoggerProvider(_options, compositeSink, _sinks);
        }
    }
}
