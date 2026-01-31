using CustomLogger.Configurations;
using CustomLogger.Providers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace CustomLogger.Loggers
{
    public static class LoggerHelper
    {
        private static ILoggerFactory _factory;

        public static void Initialize(Action<CustomProviderOptions> configure = null)
        {
            if (_factory != null)
                throw new InvalidOperationException("Logger já inicializado");

            _factory = CustomLoggerProviderExtensions.CreateCustomLoggerFactory(configure);
        }

        public static ILogger CreateLogger<T>()
        {
            if (_factory == null)
                throw new InvalidOperationException("Logger não inicializado. Chame Initialize() primeiro.");

            return _factory.CreateLogger<T>();
        }

        public static ILogger CreateLogger(string categoryName)
        {
            if (_factory == null)
                throw new InvalidOperationException("Logger não inicializado. Chame Initialize() primeiro.");

            return _factory.CreateLogger(categoryName);
        }

        public static void Shutdown()
        {
            _factory?.Dispose();
            _factory = null;
        }
    }
}
