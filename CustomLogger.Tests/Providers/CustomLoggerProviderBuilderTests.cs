using CustomLogger.Configurations;
using CustomLogger.Providers;
using Microsoft.Extensions.Logging;
using System;
using Xunit;

namespace CustomLogger.Tests.Providers
{
    /// <summary>
    /// Testes unitários para CustomLoggerProviderBuilder.ValidateConfiguration().
    /// 
    /// Estratégia: Cobertura de 80-90% via testes de regras de validação.
    /// Meta: CRAP Score < 20, Branch Coverage >= 70%
    /// Testa via fluxo público BuildApplication() que aciona ValidateConfiguration().
    /// </summary>
    public class CustomLoggerProviderBuilderTests
    {
        #region Test Helpers

        /// <summary>
        /// Cria LoggingOptions com valores customizáveis.
        /// Valores padrão garantem configuração mínima válida.
        /// </summary>
        private LoggingOptions BuildLoggingOptions(
            LogLevel? minimumLogLevel = null,
            string serviceName = "TestService",
            string environment = "Development",
            BufferOptions bufferOptions = null,
            BatchOptions batchOptions = null,
            SinkOptions sinkOptions = null)
        {
            return new LoggingOptions(
                minimumLogLevel ?? LogLevel.Information,
                serviceName,
                environment,
                bufferOptions,
                batchOptions,
                sinkOptions
            );
        }

        /// <summary>
        /// Cria LoggingOptions totalmente válida com Console sink habilitado.
        /// </summary>
        private LoggingOptions BuildValidLoggingOptions()
        {
            return BuildLoggingOptions(
                serviceName: "ValidService",
                environment: "Production",
                sinkOptions: new SinkOptions(
                    console: new ConsoleSinkOptions(true),
                    file: null,
                    blobStorage: null,
                    dynatrace: null
                )
            );
        }

        #endregion

        #region CATEGORIA 1: Caminho Feliz (1 teste)

        [Fact]
        public void BuildApplication_WithValidConfiguration_BuildsSuccessfully()
        {
            // Arrange: Configuração totalmente válida
            var loggingOptions = BuildValidLoggingOptions();
            var builder = new CustomLoggerProviderBuilder()
                .WithLoggingOptions(loggingOptions);

            // Act: BuildApplication aciona ValidateConfiguration
            var provider = builder.BuildApplication();

            // Assert
            Assert.NotNull(provider);
        }

        #endregion

        #region CATEGORIA 2: Campos Obrigatórios (2 testes)

        [Fact]
        public void BuildApplication_WithMissingServiceName_ThrowsInvalidOperationException()
        {
            // Arrange: ServiceName ausente
            var loggingOptions = BuildLoggingOptions(
                serviceName: null,
                sinkOptions: new SinkOptions(new ConsoleSinkOptions(true), null, null, null)
            );
            var builder = new CustomLoggerProviderBuilder()
                .WithLoggingOptions(loggingOptions);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => builder.BuildApplication()
            );

            Assert.Contains("ServiceName não pode ser Nulo", exception.Message);
        }

        [Fact]
        public void BuildApplication_WithMissingEnvironment_ThrowsInvalidOperationException()
        {
            // Arrange: Environment ausente
            var loggingOptions = BuildLoggingOptions(
                environment: null,
                sinkOptions: new SinkOptions(new ConsoleSinkOptions(true), null, null, null)
            );
            var builder = new CustomLoggerProviderBuilder()
                .WithLoggingOptions(loggingOptions);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => builder.BuildApplication()
            );

            Assert.Contains("Environment não pode ser Nulo", exception.Message);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void BuildApplication_WithWhitespaceServiceName_ThrowsInvalidOperationException(string serviceName)
        {
            // Arrange: ServiceName com whitespace
            var loggingOptions = BuildLoggingOptions(
                serviceName: serviceName,
                sinkOptions: new SinkOptions(new ConsoleSinkOptions(true), null, null, null)
            );
            var builder = new CustomLoggerProviderBuilder()
                .WithLoggingOptions(loggingOptions);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => builder.BuildApplication()
            );

            Assert.Contains("ServiceName não pode ser Nulo", exception.Message);
        }

        #endregion

        #region CATEGORIA 3: Buffer e Batch (3 testes)

        [Fact]
        public void BuildApplication_WithBufferEnabledAndInvalidMaxSize_ThrowsInvalidOperationException()
        {
            // Arrange: Buffer habilitado com MaxSize ≤ 0
            var loggingOptions = BuildLoggingOptions(
                bufferOptions: new BufferOptions(enabled: true, maxSize: -10),
                sinkOptions: new SinkOptions(new ConsoleSinkOptions(true), null, null, null)
            );
            var builder = new CustomLoggerProviderBuilder()
                .WithLoggingOptions(loggingOptions);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => builder.BuildApplication()
            );

            Assert.Contains("BufferOptions.MaxSize deve ser maior que zero", exception.Message);
        }

        [Fact]
        public void BuildApplication_WithBufferEnabledAndInvalidBatchSize_ThrowsInvalidOperationException()
        {
            // Arrange: Buffer habilitado + BatchSize ≤ 0 (regra cruzada)
            var loggingOptions = BuildLoggingOptions(
                bufferOptions: new BufferOptions(enabled: true, maxSize: 100),
                batchOptions: new BatchOptions(batchSize: -5, flushIntervalMs: 5000),
                sinkOptions: new SinkOptions(new ConsoleSinkOptions(true), null, null, null)
            );
            var builder = new CustomLoggerProviderBuilder()
                .WithLoggingOptions(loggingOptions);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => builder.BuildApplication()
            );

            Assert.Contains("BatchOptions.BatchSize deve ser maior que zero", exception.Message);
        }

        #endregion

        #region CATEGORIA 4: File Sink (2 testes)

        [Fact]
        public void BuildApplication_WithFileEnabledButNoPath_ThrowsInvalidOperationException()
        {
            // Arrange: File sink habilitado sem Path
            var loggingOptions = BuildLoggingOptions(
                sinkOptions: new SinkOptions(
                    console: null,
                    file: new FileSinkOptions(enabled: true, path: null),
                    blobStorage: null,
                    dynatrace: null
                )
            );
            var builder = new CustomLoggerProviderBuilder()
                .WithLoggingOptions(loggingOptions);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => builder.BuildApplication()
            );

            Assert.Contains("FileSinkOptions.Path não pode ser vazio", exception.Message);
        }

        [Fact]
        public void BuildApplication_WithFileDisabled_DoesNotValidatePath()
        {
            // Arrange: File sink desabilitado (validação deve ser pulada)
            var loggingOptions = BuildLoggingOptions(
                sinkOptions: new SinkOptions(
                    console: new ConsoleSinkOptions(true),
                    file: new FileSinkOptions(enabled: false, path: null),
                    blobStorage: null,
                    dynatrace: null
                )
            );
            var builder = new CustomLoggerProviderBuilder()
                .WithLoggingOptions(loggingOptions);

            // Act: Não deve lançar exceção
            var provider = builder.BuildApplication();

            // Assert
            Assert.NotNull(provider);
        }

        #endregion

        #region CATEGORIA 5: BlobStorage Sink (3 testes)

        [Fact]
        public void BuildApplication_WithBlobStorageEnabledButNoConnectionString_ThrowsInvalidOperationException()
        {
            // Arrange: BlobStorage habilitado sem ConnectionString
            var loggingOptions = BuildLoggingOptions(
                sinkOptions: new SinkOptions(
                    console: null,
                    file: null,
                    blobStorage: new BlobStorageSinkOptions(
                        enabled: true,
                        connectionString: null,
                        containerName: "logs"
                    ),
                    dynatrace: null
                )
            );
            var builder = new CustomLoggerProviderBuilder()
                .WithLoggingOptions(loggingOptions);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => builder.BuildApplication()
            );

            Assert.Contains("BlobStorageSinkOptions.ConnectionString não pode ser vazio", exception.Message);
        }

        [Fact]
        public void BuildApplication_WithBlobStorageEnabledButNoContainerName_ThrowsInvalidOperationException()
        {
            // Arrange: BlobStorage habilitado sem ContainerName
            var loggingOptions = BuildLoggingOptions(
                sinkOptions: new SinkOptions(
                    console: null,
                    file: null,
                    blobStorage: new BlobStorageSinkOptions(
                        enabled: true,
                        connectionString: "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=key;",
                        containerName: null
                    ),
                    dynatrace: null
                )
            );
            var builder = new CustomLoggerProviderBuilder()
                .WithLoggingOptions(loggingOptions);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => builder.BuildApplication()
            );

            Assert.Contains("BlobStorageSinkOptions.ContainerName não pode ser vazio", exception.Message);
        }

        [Fact]
        public void BuildApplication_WithBlobStorageDisabled_DoesNotValidateConfiguration()
        {
            // Arrange: BlobStorage desabilitado (validação deve ser pulada)
            var loggingOptions = BuildLoggingOptions(
                sinkOptions: new SinkOptions(
                    console: new ConsoleSinkOptions(true),
                    file: null,
                    blobStorage: new BlobStorageSinkOptions(
                        enabled: false,
                        connectionString: null,
                        containerName: null
                    ),
                    dynatrace: null
                )
            );
            var builder = new CustomLoggerProviderBuilder()
                .WithLoggingOptions(loggingOptions);

            // Act: Não deve lançar exceção
            var provider = builder.BuildApplication();

            // Assert
            Assert.NotNull(provider);
        }

        #endregion

        #region CATEGORIA 6: Dynatrace Sink (4 testes)

        [Fact]
        public void BuildApplication_WithDynatraceEnabledButNoEndpoint_ThrowsInvalidOperationException()
        {
            // Arrange: Dynatrace habilitado sem Endpoint
            var loggingOptions = BuildLoggingOptions(
                sinkOptions: new SinkOptions(
                    console: null,
                    file: null,
                    blobStorage: null,
                    dynatrace: new DynatraceSinkOptions(
                        enabled: true,
                        endpoint: null,
                        apiToken: "dt0c01.ABC123",
                        timeoutSeconds: 5
                    )
                )
            );
            var builder = new CustomLoggerProviderBuilder()
                .WithLoggingOptions(loggingOptions);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => builder.BuildApplication()
            );

            Assert.Contains("DynatraceSinkOptions.Endpoint não pode ser vazio", exception.Message);
        }

        [Fact]
        public void BuildApplication_WithDynatraceEnabledButNoApiToken_ThrowsInvalidOperationException()
        {
            // Arrange: Dynatrace habilitado sem ApiToken
            var loggingOptions = BuildLoggingOptions(
                sinkOptions: new SinkOptions(
                    console: null,
                    file: null,
                    blobStorage: null,
                    dynatrace: new DynatraceSinkOptions(
                        enabled: true,
                        endpoint: "https://dynatrace.example.com",
                        apiToken: null,
                        timeoutSeconds: 5
                    )
                )
            );
            var builder = new CustomLoggerProviderBuilder()
                .WithLoggingOptions(loggingOptions);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => builder.BuildApplication()
            );

            Assert.Contains("DynatraceSinkOptions.ApiToken não pode ser vazio", exception.Message);
        }

        [Fact]
        public void BuildApplication_WithDynatraceEnabledAndInvalidTimeout_ThrowsInvalidOperationException()
        {
            // Arrange: Dynatrace habilitado com TimeoutSeconds ≤ 0
            var loggingOptions = BuildLoggingOptions(
                sinkOptions: new SinkOptions(
                    console: null,
                    file: null,
                    blobStorage: null,
                    dynatrace: new DynatraceSinkOptions(
                        enabled: true,
                        endpoint: "https://dynatrace.example.com",
                        apiToken: "dt0c01.ABC123",
                        timeoutSeconds: -10
                    )
                )
            );
            var builder = new CustomLoggerProviderBuilder()
                .WithLoggingOptions(loggingOptions);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => builder.BuildApplication()
            );

            Assert.Contains("DynatraceSinkOptions.TimeoutSeconds deve ser maior que zero", exception.Message);
        }

        [Fact]
        public void BuildApplication_WithDynatraceDisabled_DoesNotValidateConfiguration()
        {
            // Arrange: Dynatrace desabilitado (validação deve ser pulada)
            var loggingOptions = BuildLoggingOptions(
                sinkOptions: new SinkOptions(
                    console: new ConsoleSinkOptions(true),
                    file: null,
                    blobStorage: null,
                    dynatrace: new DynatraceSinkOptions(
                        enabled: false,
                        endpoint: null,
                        apiToken: null,
                        timeoutSeconds: null
                    )
                )
            );
            var builder = new CustomLoggerProviderBuilder()
                .WithLoggingOptions(loggingOptions);

            // Act: Não deve lançar exceção
            var provider = builder.BuildApplication();

            // Assert
            Assert.NotNull(provider);
        }

        #endregion

        #region CATEGORIA 7: Múltiplos Sinks (2 testes)

        [Fact]
        public void BuildApplication_WithMultipleValidSinks_BuildsSuccessfully()
        {
            // Arrange: Múltiplos sinks habilitados e válidos
            // NOTA: BlobStorage desabilitado para evitar validação de connection string pelo Azure SDK
            var loggingOptions = BuildLoggingOptions(
                sinkOptions: new SinkOptions(
                    console: new ConsoleSinkOptions(true),
                    file: new FileSinkOptions(enabled: true, path: "/logs/app.log"),
                    blobStorage: new BlobStorageSinkOptions(
                        enabled: false,  // Desabilitado para evitar validação do SDK
                        connectionString: null,
                        containerName: null
                    ),
                    dynatrace: new DynatraceSinkOptions(
                        enabled: true,
                        endpoint: "https://dynatrace.example.com",
                        apiToken: "dt0c01.ABC123",
                        timeoutSeconds: 30
                    )
                )
            );
            var builder = new CustomLoggerProviderBuilder()
                .WithLoggingOptions(loggingOptions);

            // Act
            var provider = builder.BuildApplication();

            // Assert
            Assert.NotNull(provider);
        }

        [Fact]
        public void BuildApplication_WithNoSinksEnabled_BuildsSuccessfullyButFailsAtSinkCreation()
        {
            // Arrange: Nenhum sink habilitado
            // NOTA: ValidateConfiguration não valida se há sinks, apenas valida sinks individuais quando habilitados
            // A validação "nenhum sink" acontece em CreateSinksFromOptions
            var loggingOptions = BuildLoggingOptions(
                sinkOptions: new SinkOptions(
                    console: new ConsoleSinkOptions(false),
                    file: null,
                    blobStorage: null,
                    dynatrace: null
                )
            );
            var builder = new CustomLoggerProviderBuilder()
                .WithLoggingOptions(loggingOptions);

            // Act & Assert: Validação passa, mas BuildApplication falha em CreateSinksFromOptions
            var exception = Assert.Throws<InvalidOperationException>(
                () => builder.BuildApplication()
            );

            Assert.Contains("Nenhum sink configurado", exception.Message);
        }

        #endregion

        #region TESTES ADICIONAIS (Prioridade MÉDIA) - Edge Cases

        [Fact]
        public void BuildApplication_WithBufferDisabledAndInvalidBatch_DoesNotValidateBatch()
        {
            // Arrange: Buffer desabilitado, então BatchOptions não deve ser validado
            var loggingOptions = BuildLoggingOptions(
                bufferOptions: new BufferOptions(enabled: false, maxSize: 100),
                batchOptions: new BatchOptions(batchSize: -999, flushIntervalMs: 5000),
                sinkOptions: new SinkOptions(new ConsoleSinkOptions(true), null, null, null)
            );
            var builder = new CustomLoggerProviderBuilder()
                .WithLoggingOptions(loggingOptions);

            // Act: Não deve lançar exceção (validação de Batch é pulada)
            var provider = builder.BuildApplication();

            // Assert
            Assert.NotNull(provider);
        }

        [Fact]
        public void BuildApplication_WithCompleteValidConfiguration_BuildsSuccessfully()
        {
            // Arrange: Configuração completa com todas as opções válidas
            // NOTA: BlobStorage desabilitado para evitar validação de connection string pelo Azure SDK
            var loggingOptions = BuildLoggingOptions(
                minimumLogLevel: LogLevel.Debug,
                serviceName: "CompleteService",
                environment: "Production",
                bufferOptions: new BufferOptions(enabled: true, maxSize: 1000),
                batchOptions: new BatchOptions(batchSize: 50, flushIntervalMs: 3000),
                sinkOptions: new SinkOptions(
                    console: new ConsoleSinkOptions(true),
                    file: new FileSinkOptions(enabled: true, path: "/var/logs/app.log"),
                    blobStorage: new BlobStorageSinkOptions(
                        enabled: false,  // Desabilitado para evitar validação do SDK
                        connectionString: null,
                        containerName: null
                    ),
                    dynatrace: new DynatraceSinkOptions(
                        enabled: true,
                        endpoint: "https://abc123.live.dynatrace.com/api/v2/logs/ingest",
                        apiToken: "dt0c01.PRODUCTION.TOKEN",
                        timeoutSeconds: 10
                    )
                )
            );
            var builder = new CustomLoggerProviderBuilder()
                .WithLoggingOptions(loggingOptions);

            // Act
            var provider = builder.BuildApplication();

            // Assert
            Assert.NotNull(provider);
        }

        #endregion
    }
}