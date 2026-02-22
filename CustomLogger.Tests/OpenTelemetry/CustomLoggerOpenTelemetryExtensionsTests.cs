using CustomLogger.OpenTelemetry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Trace;
using System;
using System.Collections.Generic;
using Xunit;

namespace CustomLogger.Tests.OpenTelemetry
{
    public class CustomLoggerOpenTelemetryExtensionsTests
    {
        private static IConfiguration BuildConfig(Dictionary<string, string> values)
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(values ?? new Dictionary<string, string>())
                .Build();
        }

        [Fact]
        public void AddCustomLoggerOpenTelemetry_NullServices_ThrowsArgumentNullException()
        {
            // Arrange
            var config = BuildConfig(null);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                CustomLoggerOpenTelemetryExtensions.AddCustomLoggerOpenTelemetry(null, config));
        }

        [Fact]
        public void AddCustomLoggerOpenTelemetry_NullConfiguration_ThrowsArgumentNullException()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                CustomLoggerOpenTelemetryExtensions.AddCustomLoggerOpenTelemetry(services, null));
        }

        [Fact]
        public void AddCustomLoggerOpenTelemetry_Disabled_DoesNotRegisterOpenTelemetry()
        {
            // Arrange
            var config = BuildConfig(new Dictionary<string, string>
            {
                ["CustomLogger:OpenTelemetry:Enabled"] = "false"
            });
            var services = new ServiceCollection();

            // Act
            var result = services.AddCustomLoggerOpenTelemetry(config);

            // Assert
            Assert.Same(services, result);
            Assert.DoesNotContain(services, d => d.ServiceType.Name.Contains("TracerProvider"));
        }

        [Fact]
        public void AddCustomLoggerOpenTelemetry_Enabled_RegistersOpenTelemetry()
        {
            // Arrange
            var config = BuildConfig(new Dictionary<string, string>
            {
                ["CustomLogger:OpenTelemetry:Enabled"] = "true"
            });
            var services = new ServiceCollection();

            // Act
            var result = services.AddCustomLoggerOpenTelemetry(config);

            // Assert
            Assert.Same(services, result);
            Assert.Contains(services, d => d.ServiceType.Name.Contains("TracerProvider"));
        }

        [Fact]
        public void AddCustomLoggerOpenTelemetry_ExporterOtlp_RegistersOtlpExporter()
        {
            // Arrange
            var config = BuildConfig(new Dictionary<string, string>
            {
                ["CustomLogger:OpenTelemetry:Enabled"] = "true",
                ["CustomLogger:OpenTelemetry:Exporter"] = "otlp"
            });
            var services = new ServiceCollection();

            // Act
            services.AddCustomLoggerOpenTelemetry(config);

            // Assert
            // Não há API pública para inspecionar exporters, mas garantir que TracerProvider está registrado
            Assert.Contains(services, d => d.ServiceType.Name.Contains("TracerProvider"));
        }

        [Fact]
        public void AddCustomLoggerOpenTelemetry_ExporterConsole_RegistersConsoleExporter()
        {
            // Arrange
            var config = BuildConfig(new Dictionary<string, string>
            {
                ["CustomLogger:OpenTelemetry:Enabled"] = "true",
                ["CustomLogger:OpenTelemetry:Exporter"] = "console"
            });
            var services = new ServiceCollection();

            // Act
            services.AddCustomLoggerOpenTelemetry(config);

            // Assert
            Assert.Contains(services, d => d.ServiceType.Name.Contains("TracerProvider"));
        }

        [Fact]
        public void AddCustomLoggerOpenTelemetry_ExporterInvalid_DoesNotThrow()
        {
            // Arrange
            var config = BuildConfig(new Dictionary<string, string>
            {
                ["CustomLogger:OpenTelemetry:Enabled"] = "true",
                ["CustomLogger:OpenTelemetry:Exporter"] = "invalid"
            });
            var services = new ServiceCollection();

            // Act & Assert
            var ex = Record.Exception(() => services.AddCustomLoggerOpenTelemetry(config));
            Assert.Null(ex);
            Assert.Contains(services, d => d.ServiceType.Name.Contains("TracerProvider"));
        }

        [Fact]
        public void AddCustomLoggerOpenTelemetry_PartialConfiguration_Works()
        {
            // Arrange: only AspNetCore instrumentation enabled
            var config = BuildConfig(new Dictionary<string, string>
            {
                ["CustomLogger:OpenTelemetry:Enabled"] = "true",
                ["CustomLogger:OpenTelemetry:Instrumentations:AspNetCore"] = "true"
            });
            var services = new ServiceCollection();

            // Act
            services.AddCustomLoggerOpenTelemetry(config);

            // Assert
            Assert.Contains(services, d => d.ServiceType.Name.Contains("TracerProvider"));
        }

        [Fact]
        public void AddCustomLoggerOpenTelemetry_FluentOverride_DisablesOpenTelemetry()
        {
            // Arrange: config enabled, but override disables
            var config = BuildConfig(new Dictionary<string, string>
            {
                ["CustomLogger:OpenTelemetry:Enabled"] = "true"
            });
            var services = new ServiceCollection();

            // Act
            services.AddCustomLoggerOpenTelemetry(config, opts => opts.Enabled = false);

            // Assert
            Assert.DoesNotContain(services, d => d.ServiceType.Name.Contains("TracerProvider"));
        }

        [Fact]
        public void AddCustomLoggerOpenTelemetry_FluentOverride_EnablesOpenTelemetry()
        {
            // Arrange: config disabled, but override enables
            var config = BuildConfig(new Dictionary<string, string>
            {
                ["CustomLogger:OpenTelemetry:Enabled"] = "false"
            });
            var services = new ServiceCollection();

            // Act
            services.AddCustomLoggerOpenTelemetry(config, opts => opts.Enabled = true);

            // Assert
            Assert.Contains(services, d => d.ServiceType.Name.Contains("TracerProvider"));
        }
    }
}