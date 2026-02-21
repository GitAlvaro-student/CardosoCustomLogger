using CustomLogger.Adapters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomLogger.Tests.Adapters
{
    /// <summary>
    /// Testes unitários para CoreConfigurationAdapter.
    /// 
    /// Estratégia: Cobertura de 70-80% via testes de comportamento público.
    /// Meta: CRAP Score < 20, Branch Coverage >= 60%
    /// </summary>
    public class CoreConfigurationAdapterTests
    {
        #region Test Helpers

        /// <summary>
        /// Cria IConfiguration a partir de um dicionário de configurações.
        /// </summary>
        private IConfiguration BuildConfiguration(Dictionary<string, string> settings)
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();
        }

        /// <summary>
        /// Cria IConfiguration vazia (sem seção CustomLogger).
        /// </summary>
        private IConfiguration BuildEmptyConfiguration()
        {
            return BuildConfiguration(new Dictionary<string, string>());
        }

        #endregion

        #region CATEGORIA 1: Validação (2 testes)

        [Fact]
        public void CreateFromConfiguration_WithNullConfiguration_ThrowsArgumentNullException()
        {
            // Arrange
            var adapter = new CoreConfigurationAdapter();

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(
                () => adapter.CreateFromConfiguration(null)
            );

            Assert.Equal("configuration", exception.ParamName);
        }

        [Fact]
        public void CreateFromConfiguration_WithEmptyConfiguration_ReturnsLoggingOptionsWithNullFields()
        {
            // Arrange
            var config = BuildEmptyConfiguration();
            var adapter = new CoreConfigurationAdapter();

            // Act
            var result = adapter.CreateFromConfiguration(config);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.MinimumLogLevel);
            Assert.Null(result.ServiceName);
            Assert.Null(result.Environment);
            Assert.Null(result.BufferOptions);
            Assert.Null(result.SinkOptions);
            Assert.Null(result.BatchOptions);
        }

        #endregion

        #region CATEGORIA 2: Primitivas (3 testes)

        [Fact]
        public void CreateFromConfiguration_WithValidMinimumLogLevel_ParsesCorrectly()
        {
            // Arrange
            var config = BuildConfiguration(new Dictionary<string, string>
            {
                ["CustomLogger:MinimumLogLevel"] = "Debug"
            });
            var adapter = new CoreConfigurationAdapter();

            // Act
            var result = adapter.CreateFromConfiguration(config);

            // Assert
            Assert.Equal(LogLevel.Debug, result.MinimumLogLevel);
        }

        [Fact]
        public void CreateFromConfiguration_WithInvalidMinimumLogLevel_ReturnsNull()
        {
            // Arrange
            var config = BuildConfiguration(new Dictionary<string, string>
            {
                ["CustomLogger:MinimumLogLevel"] = "InvalidLevel"
            });
            var adapter = new CoreConfigurationAdapter();

            // Act
            var result = adapter.CreateFromConfiguration(config);

            // Assert
            Assert.Null(result.MinimumLogLevel);
        }

        [Fact]
        public void CreateFromConfiguration_WithServiceNameAndEnvironment_ParsesCorrectly()
        {
            // Arrange
            var config = BuildConfiguration(new Dictionary<string, string>
            {
                ["CustomLogger:ServiceName"] = "MyService",
                ["CustomLogger:Environment"] = "Production"
            });
            var adapter = new CoreConfigurationAdapter();

            // Act
            var result = adapter.CreateFromConfiguration(config);

            // Assert
            Assert.Equal("MyService", result.ServiceName);
            Assert.Equal("Production", result.Environment);
        }

        #endregion

        #region CATEGORIA 3: Buffer (3 testes)

        [Fact]
        public void CreateFromConfiguration_WithoutBufferSection_ReturnsNullBufferOptions()
        {
            // Arrange
            var config = BuildConfiguration(new Dictionary<string, string>
            {
                ["CustomLogger:ServiceName"] = "TestService"
            });
            var adapter = new CoreConfigurationAdapter();

            // Act
            var result = adapter.CreateFromConfiguration(config);

            // Assert
            Assert.Null(result.BufferOptions);
        }

        [Fact]
        public void CreateFromConfiguration_WithValidBufferValues_ParsesCorrectly()
        {
            // Arrange
            var config = BuildConfiguration(new Dictionary<string, string>
            {
                ["CustomLogger:Buffer:Enabled"] = "true",
                ["CustomLogger:Buffer:MaxSize"] = "1000"
            });
            var adapter = new CoreConfigurationAdapter();

            // Act
            var result = adapter.CreateFromConfiguration(config);

            // Assert
            Assert.NotNull(result.BufferOptions);
            Assert.True(result.BufferOptions.Enabled);
            Assert.Equal(1000, result.BufferOptions.MaxSize);
        }

        [Fact]
        public void CreateFromConfiguration_WithInvalidBufferValues_ReturnsNullFields()
        {
            // Arrange
            var config = BuildConfiguration(new Dictionary<string, string>
            {
                ["CustomLogger:Buffer:Enabled"] = "not-a-bool",
                ["CustomLogger:Buffer:MaxSize"] = "not-a-number"
            });
            var adapter = new CoreConfigurationAdapter();

            // Act
            var result = adapter.CreateFromConfiguration(config);

            // Assert
            Assert.NotNull(result.BufferOptions);
            Assert.Null(result.BufferOptions.Enabled);
            Assert.Equal(50, result.BufferOptions.MaxSize); // Invalid int → null → default 50 no construtor
        }

        #endregion

        #region CATEGORIA 4: Sinks - Combinações (6 testes)

        [Fact]
        public void CreateFromConfiguration_WithoutSinksSection_ReturnsNullSinkOptions()
        {
            // Arrange
            var config = BuildConfiguration(new Dictionary<string, string>
            {
                ["CustomLogger:ServiceName"] = "TestService"
            });
            var adapter = new CoreConfigurationAdapter();

            // Act
            var result = adapter.CreateFromConfiguration(config);

            // Assert
            Assert.Null(result.SinkOptions);
        }

        [Fact]
        public void CreateFromConfiguration_WithOnlyConsoleConfigured_ParsesConsoleSink()
        {
            // Arrange
            var config = BuildConfiguration(new Dictionary<string, string>
            {
                ["CustomLogger:Sinks:Console:Enabled"] = "true"
            });
            var adapter = new CoreConfigurationAdapter();

            // Act
            var result = adapter.CreateFromConfiguration(config);

            // Assert
            Assert.NotNull(result.SinkOptions);
            Assert.NotNull(result.SinkOptions.Console);
            Assert.True(result.SinkOptions.Console.Enabled);
            Assert.Null(result.SinkOptions.File);
            Assert.Null(result.SinkOptions.BlobStorage);
            Assert.Null(result.SinkOptions.Dynatrace);
        }

        [Fact]
        public void CreateFromConfiguration_WithOnlyFileConfigured_ParsesFileSink()
        {
            // Arrange
            var config = BuildConfiguration(new Dictionary<string, string>
            {
                ["CustomLogger:Sinks:File:Enabled"] = "true",
                ["CustomLogger:Sinks:File:Path"] = "/logs/app.log"
            });
            var adapter = new CoreConfigurationAdapter();

            // Act
            var result = adapter.CreateFromConfiguration(config);

            // Assert
            Assert.NotNull(result.SinkOptions);
            Assert.Null(result.SinkOptions.Console);
            Assert.NotNull(result.SinkOptions.File);
            Assert.True(result.SinkOptions.File.Enabled);
            Assert.Equal("/logs/app.log", result.SinkOptions.File.Path);
        }

        [Fact]
        public void CreateFromConfiguration_WithConsoleAndFileConfigured_ParsesBothSinks()
        {
            // Arrange
            var config = BuildConfiguration(new Dictionary<string, string>
            {
                ["CustomLogger:Sinks:Console:Enabled"] = "true",
                ["CustomLogger:Sinks:File:Enabled"] = "false",
                ["CustomLogger:Sinks:File:Path"] = "/logs/app.log"
            });
            var adapter = new CoreConfigurationAdapter();

            // Act
            var result = adapter.CreateFromConfiguration(config);

            // Assert
            Assert.NotNull(result.SinkOptions);
            Assert.NotNull(result.SinkOptions.Console);
            Assert.True(result.SinkOptions.Console.Enabled);
            Assert.NotNull(result.SinkOptions.File);
            Assert.False(result.SinkOptions.File.Enabled);
            Assert.Equal("/logs/app.log", result.SinkOptions.File.Path);
        }

        [Fact]
        public void CreateFromConfiguration_WithAllSinksConfigured_ParsesAllSinks()
        {
            // Arrange
            var config = BuildConfiguration(new Dictionary<string, string>
            {
                ["CustomLogger:Sinks:Console:Enabled"] = "true",
                ["CustomLogger:Sinks:File:Enabled"] = "true",
                ["CustomLogger:Sinks:File:Path"] = "/logs/app.log",
                ["CustomLogger:Sinks:BlobStorage:Enabled"] = "true",
                ["CustomLogger:Sinks:BlobStorage:ConnectionString"] = "DefaultEndpointsProtocol=https;...",
                ["CustomLogger:Sinks:BlobStorage:ContainerName"] = "logs",
                ["CustomLogger:Sinks:Dynatrace:Enabled"] = "true",
                ["CustomLogger:Sinks:Dynatrace:Endpoint"] = "https://dynatrace.example.com",
                ["CustomLogger:Sinks:Dynatrace:ApiToken"] = "dt0c01.ABC123",
                ["CustomLogger:Sinks:Dynatrace:TimeoutSeconds"] = "30"
            });
            var adapter = new CoreConfigurationAdapter();

            // Act
            var result = adapter.CreateFromConfiguration(config);

            // Assert
            Assert.NotNull(result.SinkOptions);

            // Console
            Assert.NotNull(result.SinkOptions.Console);
            Assert.True(result.SinkOptions.Console.Enabled);

            // File
            Assert.NotNull(result.SinkOptions.File);
            Assert.True(result.SinkOptions.File.Enabled);
            Assert.Equal("/logs/app.log", result.SinkOptions.File.Path);

            // BlobStorage
            Assert.NotNull(result.SinkOptions.BlobStorage);
            Assert.True(result.SinkOptions.BlobStorage.Enabled);
            Assert.Equal("DefaultEndpointsProtocol=https;...", result.SinkOptions.BlobStorage.ConnectionString);
            Assert.Equal("logs", result.SinkOptions.BlobStorage.ContainerName);

            // Dynatrace
            Assert.NotNull(result.SinkOptions.Dynatrace);
            Assert.True(result.SinkOptions.Dynatrace.Enabled);
            Assert.Equal("https://dynatrace.example.com", result.SinkOptions.Dynatrace.Endpoint);
            Assert.Equal("dt0c01.ABC123", result.SinkOptions.Dynatrace.ApiToken);
            Assert.Equal(30, result.SinkOptions.Dynatrace.TimeoutSeconds);
        }

        [Fact]
        public void CreateFromConfiguration_WithInvalidSinkValues_ReturnsNullFields()
        {
            // Arrange
            var config = BuildConfiguration(new Dictionary<string, string>
            {
                ["CustomLogger:Sinks:Console:Enabled"] = "invalid-bool",
                ["CustomLogger:Sinks:Dynatrace:Enabled"] = "true",
                ["CustomLogger:Sinks:Dynatrace:TimeoutSeconds"] = "not-a-number"
            });
            var adapter = new CoreConfigurationAdapter();

            // Act
            var result = adapter.CreateFromConfiguration(config);

            // Assert
            Assert.NotNull(result.SinkOptions);
            Assert.NotNull(result.SinkOptions.Console);
            Assert.Null(result.SinkOptions.Console.Enabled); // Invalid bool → null
            Assert.NotNull(result.SinkOptions.Dynatrace);
            Assert.Equal(5, result.SinkOptions.Dynatrace.TimeoutSeconds); // Invalid int → null → default 5 no construtor
        }

        #endregion

        #region CATEGORIA 5: Batch (2 testes)

        [Fact]
        public void CreateFromConfiguration_WithoutBatchSection_ReturnsNullBatchOptions()
        {
            // Arrange
            var config = BuildConfiguration(new Dictionary<string, string>
            {
                ["CustomLogger:ServiceName"] = "TestService"
            });
            var adapter = new CoreConfigurationAdapter();

            // Act
            var result = adapter.CreateFromConfiguration(config);

            // Assert
            Assert.Null(result.BatchOptions);
        }

        [Fact]
        public void CreateFromConfiguration_WithValidBatchValues_ParsesCorrectly()
        {
            // Arrange
            var config = BuildConfiguration(new Dictionary<string, string>
            {
                ["CustomLogger:Batch:BatchSize"] = "50",
                ["CustomLogger:Batch:FlushIntervalMs"] = "5000"
            });
            var adapter = new CoreConfigurationAdapter();

            // Act
            var result = adapter.CreateFromConfiguration(config);

            // Assert
            Assert.NotNull(result.BatchOptions);
            Assert.Equal(50, result.BatchOptions.BatchSize);
            Assert.Equal(5000, result.BatchOptions.FlushIntervalMs);
        }

        #endregion

        #region CATEGORIA 6: Integração Completa (2 testes)

        [Fact]
        public void CreateFromConfiguration_WithMinimalRealisticConfiguration_ParsesCorrectly()
        {
            // Arrange: Configuração mínima viável para produção
            var config = BuildConfiguration(new Dictionary<string, string>
            {
                ["CustomLogger:MinimumLogLevel"] = "Information",
                ["CustomLogger:ServiceName"] = "MyApp",
                ["CustomLogger:Environment"] = "Production",
                ["CustomLogger:Sinks:Console:Enabled"] = "true"
            });
            var adapter = new CoreConfigurationAdapter();

            // Act
            var result = adapter.CreateFromConfiguration(config);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(LogLevel.Information, result.MinimumLogLevel);
            Assert.Equal("MyApp", result.ServiceName);
            Assert.Equal("Production", result.Environment);
            Assert.NotNull(result.SinkOptions);
            Assert.NotNull(result.SinkOptions.Console);
            Assert.True(result.SinkOptions.Console.Enabled);
            Assert.Null(result.BufferOptions);
            Assert.Null(result.BatchOptions);
        }

        [Fact]
        public void CreateFromConfiguration_WithMaximalConfiguration_ParsesAllFields()
        {
            // Arrange: Configuração completa com todas seções preenchidas
            var config = BuildConfiguration(new Dictionary<string, string>
            {
                // Primitives
                ["CustomLogger:MinimumLogLevel"] = "Debug",
                ["CustomLogger:ServiceName"] = "CompleteApp",
                ["CustomLogger:Environment"] = "Development",

                // Buffer
                ["CustomLogger:Buffer:Enabled"] = "true",
                ["CustomLogger:Buffer:MaxSize"] = "1000",

                // Sinks
                ["CustomLogger:Sinks:Console:Enabled"] = "true",
                ["CustomLogger:Sinks:File:Enabled"] = "true",
                ["CustomLogger:Sinks:File:Path"] = "/var/logs/app.log",
                ["CustomLogger:Sinks:BlobStorage:Enabled"] = "false",
                ["CustomLogger:Sinks:BlobStorage:ConnectionString"] = "UseDevelopmentStorage=true",
                ["CustomLogger:Sinks:BlobStorage:ContainerName"] = "logs",
                ["CustomLogger:Sinks:Dynatrace:Enabled"] = "true",
                ["CustomLogger:Sinks:Dynatrace:Endpoint"] = "https://abc123.live.dynatrace.com",
                ["CustomLogger:Sinks:Dynatrace:ApiToken"] = "dt0c01.ST2EY72KQINMH574WMNVI7YN.G3DFPBEJYMODIDAEX4NAM",
                ["CustomLogger:Sinks:Dynatrace:TimeoutSeconds"] = "10",

                // Batch
                ["CustomLogger:Batch:BatchSize"] = "100",
                ["CustomLogger:Batch:FlushIntervalMs"] = "3000"
            });
            var adapter = new CoreConfigurationAdapter();

            // Act
            var result = adapter.CreateFromConfiguration(config);

            // Assert
            Assert.NotNull(result);

            // Primitives
            Assert.Equal(LogLevel.Debug, result.MinimumLogLevel);
            Assert.Equal("CompleteApp", result.ServiceName);
            Assert.Equal("Development", result.Environment);

            // Buffer
            Assert.NotNull(result.BufferOptions);
            Assert.True(result.BufferOptions.Enabled);
            Assert.Equal(1000, result.BufferOptions.MaxSize);

            // Sinks
            Assert.NotNull(result.SinkOptions);
            Assert.NotNull(result.SinkOptions.Console);
            Assert.True(result.SinkOptions.Console.Enabled);
            Assert.NotNull(result.SinkOptions.File);
            Assert.True(result.SinkOptions.File.Enabled);
            Assert.Equal("/var/logs/app.log", result.SinkOptions.File.Path);
            Assert.NotNull(result.SinkOptions.BlobStorage);
            Assert.False(result.SinkOptions.BlobStorage.Enabled);
            Assert.NotNull(result.SinkOptions.Dynatrace);
            Assert.True(result.SinkOptions.Dynatrace.Enabled);
            Assert.Equal("https://abc123.live.dynatrace.com", result.SinkOptions.Dynatrace.Endpoint);
            Assert.Equal(10, result.SinkOptions.Dynatrace.TimeoutSeconds);

            // Batch
            Assert.NotNull(result.BatchOptions);
            Assert.Equal(100, result.BatchOptions.BatchSize);
            Assert.Equal(3000, result.BatchOptions.FlushIntervalMs);
        }

        #endregion

        #region TESTES ADICIONAIS (Prioridade MÉDIA/BAIXA) - Opcional para > 70% cobertura

        [Theory]
        [InlineData("Trace")]
        [InlineData("Debug")]
        [InlineData("Information")]
        [InlineData("Warning")]
        [InlineData("Error")]
        [InlineData("Critical")]
        public void CreateFromConfiguration_WithVariousLogLevels_ParsesCorrectly(string logLevelStr)
        {
            // Arrange
            var config = BuildConfiguration(new Dictionary<string, string>
            {
                ["CustomLogger:MinimumLogLevel"] = logLevelStr
            });
            var adapter = new CoreConfigurationAdapter();

            // Act
            var result = adapter.CreateFromConfiguration(config);

            // Assert
            Assert.True(Enum.TryParse<LogLevel>(logLevelStr, out var expectedLevel));
            Assert.Equal(expectedLevel, result.MinimumLogLevel);
        }

        [Fact]
        public void CreateFromConfiguration_WithWhitespaceValues_ReturnsNull()
        {
            // Arrange
            var config = BuildConfiguration(new Dictionary<string, string>
            {
                ["CustomLogger:ServiceName"] = "   ",
                ["CustomLogger:Environment"] = "",
                ["CustomLogger:Sinks:File:Path"] = "  "
            });
            var adapter = new CoreConfigurationAdapter();

            // Act
            var result = adapter.CreateFromConfiguration(config);

            // Assert
            Assert.Null(result.ServiceName);
            Assert.Null(result.Environment);
        }

        [Fact]
        public void CreateFromConfiguration_WithBlobStorageOnly_ParsesBlobStorageSink()
        {
            // Arrange
            var config = BuildConfiguration(new Dictionary<string, string>
            {
                ["CustomLogger:Sinks:BlobStorage:Enabled"] = "true",
                ["CustomLogger:Sinks:BlobStorage:ConnectionString"] = "DefaultEndpointsProtocol=https;AccountName=myaccount;AccountKey=mykey;EndpointSuffix=core.windows.net",
                ["CustomLogger:Sinks:BlobStorage:ContainerName"] = "application-logs"
            });
            var adapter = new CoreConfigurationAdapter();

            // Act
            var result = adapter.CreateFromConfiguration(config);

            // Assert
            Assert.NotNull(result.SinkOptions);
            Assert.NotNull(result.SinkOptions.BlobStorage);
            Assert.True(result.SinkOptions.BlobStorage.Enabled);
            Assert.Equal("DefaultEndpointsProtocol=https;AccountName=myaccount;AccountKey=mykey;EndpointSuffix=core.windows.net",
                result.SinkOptions.BlobStorage.ConnectionString);
            Assert.Equal("application-logs", result.SinkOptions.BlobStorage.ContainerName);
            Assert.Null(result.SinkOptions.Console);
            Assert.Null(result.SinkOptions.File);
            Assert.Null(result.SinkOptions.Dynatrace);
        }

        [Fact]
        public void CreateFromConfiguration_WithDynatraceOnly_ParsesDynatraceSink()
        {
            // Arrange
            var config = BuildConfiguration(new Dictionary<string, string>
            {
                ["CustomLogger:Sinks:Dynatrace:Enabled"] = "false",
                ["CustomLogger:Sinks:Dynatrace:Endpoint"] = "https://xyz789.live.dynatrace.com/api/v2/logs/ingest",
                ["CustomLogger:Sinks:Dynatrace:ApiToken"] = "dt0c01.SAMPLE.TOKEN",
                ["CustomLogger:Sinks:Dynatrace:TimeoutSeconds"] = "5"
            });
            var adapter = new CoreConfigurationAdapter();

            // Act
            var result = adapter.CreateFromConfiguration(config);

            // Assert
            Assert.NotNull(result.SinkOptions);
            Assert.NotNull(result.SinkOptions.Dynatrace);
            Assert.False(result.SinkOptions.Dynatrace.Enabled);
            Assert.Equal("https://xyz789.live.dynatrace.com/api/v2/logs/ingest", result.SinkOptions.Dynatrace.Endpoint);
            Assert.Equal("dt0c01.SAMPLE.TOKEN", result.SinkOptions.Dynatrace.ApiToken);
            Assert.Equal(5, result.SinkOptions.Dynatrace.TimeoutSeconds);
        }

        #endregion
    }
}
