using CustomLogger.Adapters;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace CustomLogger.Tests.Adapters
{
    public class FrameworkConfigurationAdapterTests
    {
        // Helper scope that replaces ConfigurationManager.AppSettings for the lifetime of the scope
        // and restores the original values on Dispose. Tests remain independent.
        private sealed class AppSettingsScope : IDisposable
        {
            private readonly IDictionary<string, string> _original;
            private readonly bool _hadAnyOriginal;

            public AppSettingsScope(IDictionary<string, string> replacement)
            {
                // Read current appSettings from the configuration file for this process
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var section = config.AppSettings;
                _original = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (KeyValueConfigurationElement kv in section.Settings)
                {
                    _original[kv.Key] = kv.Value;
                }

                _hadAnyOriginal = _original.Count > 0;

                // Replace: clear current settings and add replacement (if any)
                section.Settings.Clear();
                if (replacement != null)
                {
                    foreach (var kv in replacement)
                    {
                        section.Settings.Add(kv.Key, kv.Value);
                    }
                }

                // Persist and refresh so ConfigurationManager.AppSettings reflects changes
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
            }

            public void Dispose()
            {
                // Restore original section content
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var section = config.AppSettings;
                section.Settings.Clear();

                if (_hadAnyOriginal)
                {
                    foreach (var kv in _original)
                    {
                        section.Settings.Add(kv.Key, kv.Value);
                    }
                }

                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
            }
        }

        [Fact]
        public void CreateFromAppSettings_MinimalValidConfiguration_ReturnsLoggingOptionsWithRequiredValues()
        {
            // Arrange
            var settings = new Dictionary<string, string>
            {
                ["CustomLogger:ServiceName"] = "MyService",
                ["CustomLogger:Environment"] = "Prod"
            };

            using (new AppSettingsScope(settings))
            {
                var adapter = new FrameworkConfigurationAdapter();

                // Act
                var result = adapter.CreateFromAppSettings();

                // Assert
                Assert.NotNull(result);
                Assert.Equal("MyService", result.ServiceName);
                Assert.Equal("Prod", result.Environment);
                Assert.Null(result.MinimumLogLevel);
                Assert.Null(result.BufferOptions);
                Assert.Null(result.BatchOptions);
                Assert.Null(result.SinkOptions);
            }
        }

        [Fact]
        public void CreateFromAppSettings_ValidLogLevel_IsParsed()
        {
            // Arrange
            var settings = new Dictionary<string, string>
            {
                ["CustomLogger:ServiceName"] = "S",
                ["CustomLogger:Environment"] = "E",
                ["CustomLogger:MinimumLogLevel"] = "Warning"
            };

            using (new AppSettingsScope(settings))
            {
                var adapter = new FrameworkConfigurationAdapter();

                // Act
                var result = adapter.CreateFromAppSettings();

                // Assert
                Assert.NotNull(result);
                Assert.Equal(LogLevel.Warning, result.MinimumLogLevel);
            }
        }

        [Fact]
        public void CreateFromAppSettings_InvalidLogLevel_ThrowsConfigurationErrorsException()
        {
            // Arrange
            var settings = new Dictionary<string, string>
            {
                ["CustomLogger:ServiceName"] = "S",
                ["CustomLogger:Environment"] = "E",
                ["CustomLogger:MinimumLogLevel"] = "NotALevel"
            };

            using (new AppSettingsScope(settings))
            {
                var adapter = new FrameworkConfigurationAdapter();

                // Act & Assert
                Assert.Throws<ConfigurationErrorsException>(() => adapter.CreateFromAppSettings());
            }
        }

        [Fact]
        public void CreateFromAppSettings_MissingServiceName_ThrowsConfigurationErrorsException()
        {
            // Arrange (no ServiceName)
            var settings = new Dictionary<string, string>
            {
                ["CustomLogger:Environment"] = "E"
            };

            using (new AppSettingsScope(settings))
            {
                var adapter = new FrameworkConfigurationAdapter();

                // Act & Assert
                Assert.Throws<ConfigurationErrorsException>(() => adapter.CreateFromAppSettings());
            }
        }

        [Fact]
        public void CreateFromAppSettings_MissingEnvironment_ThrowsConfigurationErrorsException()
        {
            // Arrange (no Environment)
            var settings = new Dictionary<string, string>
            {
                ["CustomLogger:ServiceName"] = "S"
            };

            using (new AppSettingsScope(settings))
            {
                var adapter = new FrameworkConfigurationAdapter();

                // Act & Assert
                Assert.Throws<ConfigurationErrorsException>(() => adapter.CreateFromAppSettings());
            }
        }

        [Fact]
        public void CreateFromAppSettings_BufferOptions_PartiallyConfigured_ReturnsBufferOptionsWithDefaults()
        {
            // Arrange: only Enabled provided
            var settings = new Dictionary<string, string>
            {
                ["CustomLogger:ServiceName"] = "S",
                ["CustomLogger:Environment"] = "E",
                ["CustomLogger:Buffer:Enabled"] = "true"
            };

            using (new AppSettingsScope(settings))
            {
                var adapter = new FrameworkConfigurationAdapter();

                // Act
                var result = adapter.CreateFromAppSettings();

                // Assert
                Assert.NotNull(result.BufferOptions);
                Assert.True(result.BufferOptions.Enabled.Value);
                // MaxSize default is 50 when null passed to BufferOptions ctor
                Assert.Equal(50, result.BufferOptions.MaxSize);
            }
        }

        [Fact]
        public void CreateFromAppSettings_FileSinkConfigured_SinkOptionsContainsFile()
        {
            // Arrange: file path only (no enabled)
            var settings = new Dictionary<string, string>
            {
                ["CustomLogger:ServiceName"] = "S",
                ["CustomLogger:Environment"] = "E",
                ["CustomLogger:Sinks:File:Path"] = "/tmp/log.txt"
            };

            using (new AppSettingsScope(settings))
            {
                var adapter = new FrameworkConfigurationAdapter();

                // Act
                var result = adapter.CreateFromAppSettings();

                // Assert
                Assert.NotNull(result.SinkOptions);
                Assert.NotNull(result.SinkOptions.File);
                Assert.Equal("/tmp/log.txt", result.SinkOptions.File.Path);
                Assert.Null(result.SinkOptions.Console);
                Assert.Null(result.SinkOptions.BlobStorage);
                Assert.Null(result.SinkOptions.Dynatrace);
            }
        }

        [Fact]
        public void CreateFromAppSettings_BlobStorageConfigured_SinkOptionsContainsBlobStorage()
        {
            // Arrange
            var settings = new Dictionary<string, string>
            {
                ["CustomLogger:ServiceName"] = "S",
                ["CustomLogger:Environment"] = "E",
                ["CustomLogger:Sinks:BlobStorage:ConnectionString"] = "ConnStr",
                ["CustomLogger:Sinks:BlobStorage:ContainerName"] = "container"
            };

            using (new AppSettingsScope(settings))
            {
                var adapter = new FrameworkConfigurationAdapter();

                // Act
                var result = adapter.CreateFromAppSettings();

                // Assert
                Assert.NotNull(result.SinkOptions);
                Assert.NotNull(result.SinkOptions.BlobStorage);
                Assert.Equal("ConnStr", result.SinkOptions.BlobStorage.ConnectionString);
                Assert.Equal("container", result.SinkOptions.BlobStorage.ContainerName);
            }
        }

        [Fact]
        public void CreateFromAppSettings_DynatraceConfigured_ParsesValuesIncludingTimeout()
        {
            // Arrange
            var settings = new Dictionary<string, string>
            {
                ["CustomLogger:ServiceName"] = "S",
                ["CustomLogger:Environment"] = "E",
                ["CustomLogger:Sinks:Dynatrace:Endpoint"] = "https://dt",
                ["CustomLogger:Sinks:Dynatrace:ApiToken"] = "token",
                ["CustomLogger:Sinks:Dynatrace:TimeoutSeconds"] = "7"
            };

            using (new AppSettingsScope(settings))
            {
                var adapter = new FrameworkConfigurationAdapter();

                // Act
                var result = adapter.CreateFromAppSettings();

                // Assert
                Assert.NotNull(result.SinkOptions);
                Assert.NotNull(result.SinkOptions.Dynatrace);
                Assert.Equal("https://dt", result.SinkOptions.Dynatrace.Endpoint);
                Assert.Equal("token", result.SinkOptions.Dynatrace.ApiToken);
                Assert.Equal(7, result.SinkOptions.Dynatrace.TimeoutSeconds);
            }
        }

        [Fact]
        public void CreateFromAppSettings_BatchOptionsConfigured_ParsesValues()
        {
            // Arrange
            var settings = new Dictionary<string, string>
            {
                ["CustomLogger:ServiceName"] = "S",
                ["CustomLogger:Environment"] = "E",
                ["CustomLogger:Batch:BatchSize"] = "45",
                ["CustomLogger:Batch:FlushIntervalMs"] = "2000"
            };

            using (new AppSettingsScope(settings))
            {
                var adapter = new FrameworkConfigurationAdapter();

                // Act
                var result = adapter.CreateFromAppSettings();

                // Assert
                Assert.NotNull(result.BatchOptions);
                Assert.Equal(45, result.BatchOptions.BatchSize);
                Assert.Equal(2000, result.BatchOptions.FlushIntervalMs);
            }
        }

        [Fact]
        public void CreateFromAppSettings_InvalidBoolean_ThrowsConfigurationErrorsException()
        {
            // Arrange: invalid boolean for console enabled
            var settings = new Dictionary<string, string>
            {
                ["CustomLogger:ServiceName"] = "S",
                ["CustomLogger:Environment"] = "E",
                ["CustomLogger:Sinks:Console:Enabled"] = "notABool"
            };

            using (new AppSettingsScope(settings))
            {
                var adapter = new FrameworkConfigurationAdapter();

                // Act & Assert
                Assert.Throws<ConfigurationErrorsException>(() => adapter.CreateFromAppSettings());
            }
        }

        [Fact]
        public void CreateFromAppSettings_InvalidInt_ThrowsConfigurationErrorsException()
        {
            // Arrange: invalid int for buffer max size
            var settings = new Dictionary<string, string>
            {
                ["CustomLogger:ServiceName"] = "S",
                ["CustomLogger:Environment"] = "E",
                ["CustomLogger:Buffer:MaxSize"] = "abc"
            };

            using (new AppSettingsScope(settings))
            {
                var adapter = new FrameworkConfigurationAdapter();

                // Act & Assert
                Assert.Throws<ConfigurationErrorsException>(() => adapter.CreateFromAppSettings());
            }
        }

        [Fact]
        public void CreateFromAppSettings_NoSinkConfigured_SinkOptionsIsNull()
        {
            // Arrange: only required keys
            var settings = new Dictionary<string, string>
            {
                ["CustomLogger:ServiceName"] = "S",
                ["CustomLogger:Environment"] = "E"
            };

            using (new AppSettingsScope(settings))
            {
                var adapter = new FrameworkConfigurationAdapter();

                // Act
                var result = adapter.CreateFromAppSettings();

                // Assert
                Assert.Null(result.SinkOptions);
            }
        }

        [Fact]
        public void CreateFromAppSettings_AtLeastOneSinkConfigured_SinkOptionsIsNotNull()
        {
            // Arrange: enable console -> should create SinkOptions
            var settings = new Dictionary<string, string>
            {
                ["CustomLogger:ServiceName"] = "S",
                ["CustomLogger:Environment"] = "E",
                ["CustomLogger:Sinks:Console:Enabled"] = "true"
            };

            using (new AppSettingsScope(settings))
            {
                var adapter = new FrameworkConfigurationAdapter();

                // Act
                var result = adapter.CreateFromAppSettings();

                // Assert
                Assert.NotNull(result.SinkOptions);
                Assert.NotNull(result.SinkOptions.Console);
                Assert.True(result.SinkOptions.Console.Enabled.Value);
            }
        }
    }
}