using CustomLogger.Abstractions;
using CustomLogger.Sinks;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace CustomLogger.Tests.Sinks
{
    /// <summary>
    /// Testes unitários para DynatraceLogSink.
    /// Objetivo: validar que o sink respeita o contrato do Core e propaga exceções corretamente.
    /// </summary>
    public class DynatraceLogSinkTests
    {
        private const string TestEndpoint = "https://test.dynatrace.com/api/v2/logs";
        private const string TestApiToken = "test-token-12345";
        private const string TestJsonPayload = "{\"timestamp\":\"2026-02-08T10:00:00Z\",\"level\":\"Information\",\"message\":\"Test log\"}";

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_ShouldNotThrow()
        {
            // Arrange
            var mockFormatter = new Mock<ILogFormatter>();
            var mockHttpClient = new Mock<HttpClient>();

            // Act & Assert
            var sink = new DynatraceLogSink(TestEndpoint, TestApiToken, mockHttpClient.Object, mockFormatter.Object);
            Assert.NotNull(sink);
        }

        [Fact]
        public void Constructor_WithNullEndpoint_ShouldThrowArgumentNullException()
        {
            // Arrange
            var mockFormatter = new Mock<ILogFormatter>();
            var mockHttpClient = new Mock<HttpClient>();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new DynatraceLogSink(null, TestApiToken, mockHttpClient.Object, mockFormatter.Object));
        }

        [Fact]
        public void Constructor_WithNullApiToken_ShouldThrowArgumentNullException()
        {
            // Arrange
            var mockFormatter = new Mock<ILogFormatter>();
            var mockHttpClient = new Mock<HttpClient>();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new DynatraceLogSink(TestEndpoint, null, mockHttpClient.Object, mockFormatter.Object));
        }

        [Fact]
        public void Constructor_WithNullFormatter_ShouldThrowArgumentNullException()
        {
            // Arrange
            var mockHttpClient = new Mock<HttpClient>();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new DynatraceLogSink(TestEndpoint, TestApiToken, mockHttpClient.Object, null));
        }

        [Fact]
        public void Constructor_WithNullHttpClient_ShouldCreateDefaultClient()
        {
            // Arrange
            var mockFormatter = new Mock<ILogFormatter>();

            // Act - passa null para HttpClient, deve criar um padrão
            var sink = new DynatraceLogSink(TestEndpoint, TestApiToken, null, mockFormatter.Object);

            // Assert - sink foi criado com sucesso (cliente interno foi criado)
            Assert.NotNull(sink);
        }

        #endregion

        #region Validation Tests - Silent Failures

        [Fact]
        public void Write_WithNullEntry_ShouldNotThrow()
        {
            // Arrange
            var mockFormatter = new Mock<ILogFormatter>();
            var mockHandler = CreateMockHttpMessageHandler(HttpStatusCode.OK);
            var httpClient = new HttpClient(mockHandler.Object);
            var sink = new DynatraceLogSink(TestEndpoint, TestApiToken, httpClient, mockFormatter.Object);

            // Act & Assert - não deve lançar exceção, deve retornar silenciosamente
            sink.Write(null);

            // Verify - formatter nunca foi chamado
            mockFormatter.Verify(f => f.Format(It.IsAny<ILogEntry>()), Times.Never);

            // Verify - HttpClient nunca foi chamado
            mockHandler.Protected().Verify(
                "SendAsync",
                Times.Never(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public void Write_WithNullState_ShouldNotThrow()
        {
            var mockFormatter = new Mock<ILogFormatter>();
            var mockHandler = CreateMockHttpMessageHandler(HttpStatusCode.OK);
            var httpClient = new HttpClient(mockHandler.Object);
            var sink = new DynatraceLogSink(TestEndpoint, TestApiToken, httpClient, mockFormatter.Object);

            // Cria mock explicitamente com State = null
            var mockEntry = new Mock<ILogEntry>();
            mockEntry.Setup(e => e.State).Returns((object)null);
            mockEntry.Setup(e => e.Message).Returns("Test message");
            mockEntry.Setup(e => e.LogLevel).Returns(LogLevel.Information);
            mockEntry.Setup(e => e.Timestamp).Returns(DateTimeOffset.UtcNow);
            mockEntry.Setup(e => e.Category).Returns("TestCategory");

            // Act
            sink.Write(mockEntry.Object);

            // Assert - formatter nunca foi chamado
            mockFormatter.Verify(f => f.Format(It.IsAny<ILogEntry>()), Times.Never);

            // HttpClient nunca foi chamado
            mockHandler.Protected().Verify(
                "SendAsync",
                Times.Never(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public void Write_WithEmptyJson_ShouldNotSendRequest()
        {
            // Arrange
            var mockFormatter = new Mock<ILogFormatter>();
            mockFormatter.Setup(f => f.Format(It.IsAny<ILogEntry>())).Returns(string.Empty);

            var mockHandler = CreateMockHttpMessageHandler(HttpStatusCode.OK);
            var httpClient = new HttpClient(mockHandler.Object);
            var sink = new DynatraceLogSink(TestEndpoint, TestApiToken, httpClient, mockFormatter.Object);

            var entry = CreateMockLogEntry(state: new { Test = "data" });

            // Act
            sink.Write(entry.Object);

            // Assert - formatter foi chamado
            mockFormatter.Verify(f => f.Format(entry.Object), Times.Once);

            // Verify - HttpClient NÃO foi chamado (JSON vazio)
            mockHandler.Protected().Verify(
                "SendAsync",
                Times.Never(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public void Write_WithWhitespaceJson_ShouldNotSendRequest()
        {
            // Arrange
            var mockFormatter = new Mock<ILogFormatter>();
            mockFormatter.Setup(f => f.Format(It.IsAny<ILogEntry>())).Returns("   ");

            var mockHandler = CreateMockHttpMessageHandler(HttpStatusCode.OK);
            var httpClient = new HttpClient(mockHandler.Object);
            var sink = new DynatraceLogSink(TestEndpoint, TestApiToken, httpClient, mockFormatter.Object);

            var entry = CreateMockLogEntry(state: new { Test = "data" });

            // Act
            sink.Write(entry.Object);

            // Assert - formatter foi chamado
            mockFormatter.Verify(f => f.Format(entry.Object), Times.Once);

            // Verify - HttpClient NÃO foi chamado (JSON apenas espaços)
            mockHandler.Protected().Verify(
                "SendAsync",
                Times.Never(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
        }

        #endregion

        #region Formatter Integration Tests

        [Fact]
        public void Write_WithValidEntry_ShouldCallFormatterExactlyOnce()
        {
            // Arrange
            var mockFormatter = new Mock<ILogFormatter>();
            mockFormatter.Setup(f => f.Format(It.IsAny<ILogEntry>())).Returns(TestJsonPayload);

            var mockHandler = CreateMockHttpMessageHandler(HttpStatusCode.OK);
            var httpClient = new HttpClient(mockHandler.Object);
            var sink = new DynatraceLogSink(TestEndpoint, TestApiToken, httpClient, mockFormatter.Object);

            var entry = CreateMockLogEntry(state: new { Test = "data" });

            // Act
            sink.Write(entry.Object);

            // Assert - formatter foi chamado exatamente uma vez com a entry correta
            mockFormatter.Verify(f => f.Format(entry.Object), Times.Once);
        }

        [Fact]
        public void Write_ShouldUseFormatterJsonAsRequestBody()
        {
            var customJson = "{\"custom\":\"payload\",\"test\":true}";
            var mockFormatter = new Mock<ILogFormatter>();
            mockFormatter.Setup(f => f.Format(It.IsAny<ILogEntry>())).Returns(customJson);

            string capturedContent = null;

            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns((HttpRequestMessage request, CancellationToken ct) =>
                {
                    // Captura content DENTRO do callback
                    capturedContent = request.Content.ReadAsStringAsync().Result;
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
                });

            var httpClient = new HttpClient(mockHandler.Object);
            var sink = new DynatraceLogSink(TestEndpoint, TestApiToken, httpClient, mockFormatter.Object);

            var entry = CreateMockLogEntry(state: new { Test = "data" });
            sink.Write(entry.Object);

            Assert.Equal(customJson, capturedContent);
        }

        [Fact]
        public void Write_ShouldNotModifyFormatterOutput()
        {
            var complexJson = "{\n  \"level\": \"Error\",\n  \"message\": \"Test with \\\"quotes\\\" and special chars: àçñ\"\n}";
            var mockFormatter = new Mock<ILogFormatter>();
            mockFormatter.Setup(f => f.Format(It.IsAny<ILogEntry>())).Returns(complexJson);

            string capturedContent = null;

            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns((HttpRequestMessage request, CancellationToken ct) =>
                {
                    capturedContent = request.Content.ReadAsStringAsync().Result;
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
                });

            var httpClient = new HttpClient(mockHandler.Object);
            var sink = new DynatraceLogSink(TestEndpoint, TestApiToken, httpClient, mockFormatter.Object);

            var entry = CreateMockLogEntry(state: new { Test = "data" });
            sink.Write(entry.Object);

            Assert.Equal(complexJson, capturedContent);
        }

        #endregion

        #region HTTP Request Construction Tests

        [Fact]
        public void Write_ShouldCreatePostRequest()
        {
            // Arrange
            var mockFormatter = new Mock<ILogFormatter>();
            mockFormatter.Setup(f => f.Format(It.IsAny<ILogEntry>())).Returns(TestJsonPayload);

            HttpRequestMessage capturedRequest = null;
            var mockHandler = CreateMockHttpMessageHandler(HttpStatusCode.OK, request =>
            {
                capturedRequest = request;
            });

            var httpClient = new HttpClient(mockHandler.Object);
            var sink = new DynatraceLogSink(TestEndpoint, TestApiToken, httpClient, mockFormatter.Object);

            var entry = CreateMockLogEntry(state: new { Test = "data" });

            // Act
            sink.Write(entry.Object);

            Thread.Sleep(50);

            // Assert
            Assert.NotNull(capturedRequest);
            Assert.Equal(HttpMethod.Post, capturedRequest.Method);
        }

        [Fact]
        public void Write_ShouldUseCorrectEndpoint()
        {
            // Arrange
            var customEndpoint = "https://custom.endpoint.com/api/logs";
            var mockFormatter = new Mock<ILogFormatter>();
            mockFormatter.Setup(f => f.Format(It.IsAny<ILogEntry>())).Returns(TestJsonPayload);

            HttpRequestMessage capturedRequest = null;
            var mockHandler = CreateMockHttpMessageHandler(HttpStatusCode.OK, request =>
            {
                capturedRequest = request;
            });

            var httpClient = new HttpClient(mockHandler.Object);
            var sink = new DynatraceLogSink(customEndpoint, TestApiToken, httpClient, mockFormatter.Object);

            var entry = CreateMockLogEntry(state: new { Test = "data" });

            // Act
            sink.Write(entry.Object);

            Thread.Sleep(50);

            // Assert
            Assert.NotNull(capturedRequest);
            Assert.Equal(customEndpoint, capturedRequest.RequestUri.ToString());
        }

        [Fact]
        public void Write_ShouldSetContentTypeToApplicationJson()
        {
            // Arrange
            var mockFormatter = new Mock<ILogFormatter>();
            mockFormatter.Setup(f => f.Format(It.IsAny<ILogEntry>())).Returns(TestJsonPayload);

            HttpRequestMessage capturedRequest = null;
            var mockHandler = CreateMockHttpMessageHandler(HttpStatusCode.OK, request =>
            {
                capturedRequest = request;
            });

            var httpClient = new HttpClient(mockHandler.Object);
            var sink = new DynatraceLogSink(TestEndpoint, TestApiToken, httpClient, mockFormatter.Object);

            var entry = CreateMockLogEntry(state: new { Test = "data" });

            // Act
            sink.Write(entry.Object);

            Thread.Sleep(50);

            // Assert
            Assert.NotNull(capturedRequest);
            Assert.NotNull(capturedRequest.Content);
            Assert.Equal("application/json", capturedRequest.Content.Headers.ContentType.MediaType);
        }

        [Fact]
        public void Write_ShouldSetAuthorizationHeader()
        {
            // Arrange
            var customToken = "custom-api-token-xyz";
            var mockFormatter = new Mock<ILogFormatter>();
            mockFormatter.Setup(f => f.Format(It.IsAny<ILogEntry>())).Returns(TestJsonPayload);

            HttpRequestMessage capturedRequest = null;
            var mockHandler = CreateMockHttpMessageHandler(HttpStatusCode.OK, request =>
            {
                capturedRequest = request;
            });

            var httpClient = new HttpClient(mockHandler.Object);
            var sink = new DynatraceLogSink(TestEndpoint, customToken, httpClient, mockFormatter.Object);

            var entry = CreateMockLogEntry(state: new { Test = "data" });

            // Act
            sink.Write(entry.Object);

            Thread.Sleep(50);

            // Assert - header Authorization existe
            Assert.NotNull(capturedRequest);
            Assert.True(capturedRequest.Headers.Contains("Authorization"));

            // Verify - formato correto "Api-Token {token}"
            var authHeaderValues = capturedRequest.Headers.GetValues("Authorization");
            Assert.Contains($"Api-Token {customToken}", authHeaderValues);
        }

        [Fact]
        public void Write_ShouldCallSendAsyncExactlyOnce()
        {
            // Arrange
            var mockFormatter = new Mock<ILogFormatter>();
            mockFormatter.Setup(f => f.Format(It.IsAny<ILogEntry>())).Returns(TestJsonPayload);

            var mockHandler = CreateMockHttpMessageHandler(HttpStatusCode.OK);
            var httpClient = new HttpClient(mockHandler.Object);
            var sink = new DynatraceLogSink(TestEndpoint, TestApiToken, httpClient, mockFormatter.Object);

            var entry = CreateMockLogEntry(state: new { Test = "data" });

            // Act
            sink.Write(entry.Object);

            Thread.Sleep(50);

            // Assert - SendAsync foi chamado exatamente uma vez
            mockHandler.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public void Write_WithSuccessfulResponse_ShouldNotThrow()
        {
            // Arrange
            var mockFormatter = new Mock<ILogFormatter>();
            mockFormatter.Setup(f => f.Format(It.IsAny<ILogEntry>())).Returns(TestJsonPayload);

            var mockHandler = CreateMockHttpMessageHandler(HttpStatusCode.OK);
            var httpClient = new HttpClient(mockHandler.Object);
            var sink = new DynatraceLogSink(TestEndpoint, TestApiToken, httpClient, mockFormatter.Object);

            var entry = CreateMockLogEntry(state: new { Test = "data" });

            // Act & Assert - não deve lançar exceção
            sink.Write(entry.Object);
        }

        #endregion

        #region Dispose Tests

        [Fact]
        public void Dispose_WhenHttpClientWasProvided_ShouldNotDisposeClient()
        {
            // Arrange
            var mockFormatter = new Mock<ILogFormatter>();
            var mockHandler = CreateMockHttpMessageHandler(HttpStatusCode.OK);
            var httpClient = new HttpClient(mockHandler.Object);
            var sink = new DynatraceLogSink(TestEndpoint, TestApiToken, httpClient, mockFormatter.Object);

            // Act
            sink.Dispose();

            // Assert - HttpClient ainda deve estar utilizável (não foi disposado)
            // Tentar usar o cliente após Dispose do sink não deve lançar ObjectDisposedException
            var testRequest = new HttpRequestMessage(HttpMethod.Get, "http://test.com");
            var sendTask = httpClient.SendAsync(testRequest);

            // Se não lançar exceção, significa que o client não foi disposado
            Assert.NotNull(sendTask);
        }

        [Fact]
        public void Dispose_WhenHttpClientWasNotProvided_ShouldDisposeInternalClient()
        {
            // Arrange
            var mockFormatter = new Mock<ILogFormatter>();

            // Sink cria HttpClient internamente (null foi passado)
            var sink = new DynatraceLogSink(TestEndpoint, TestApiToken, null, mockFormatter.Object);

            // Act - não deve lançar exceção
            sink.Dispose();

            // Assert - Dispose completou com sucesso
            // Nota: não podemos acessar o cliente interno para verificar se foi disposado,
            // mas garantimos que Dispose() não lança exceção
        }

        [Fact]
        public void Dispose_CalledMultipleTimes_ShouldNotThrow()
        {
            // Arrange
            var mockFormatter = new Mock<ILogFormatter>();
            var mockHandler = CreateMockHttpMessageHandler(HttpStatusCode.OK);
            var httpClient = new HttpClient(mockHandler.Object);
            var sink = new DynatraceLogSink(TestEndpoint, TestApiToken, httpClient, mockFormatter.Object);

            // Act & Assert - múltiplas chamadas não devem lançar exceção
            sink.Dispose();
            sink.Dispose();
            sink.Dispose();
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Cria um mock de HttpMessageHandler que retorna resposta configurável.
        /// </summary>
        private Mock<HttpMessageHandler> CreateMockHttpMessageHandler(
            HttpStatusCode statusCode,
            Action<HttpRequestMessage> requestCapture = null)
        {
            var mockHandler = new Mock<HttpMessageHandler>();

            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns((HttpRequestMessage request, CancellationToken ct) =>
                {
                    // Captura request se callback fornecido
                    requestCapture?.Invoke(request);

                    // Retorna resposta com status code configurado
                    return Task.FromResult(new HttpResponseMessage(statusCode)
                    {
                        Content = new StringContent("{\"status\":\"ok\"}")
                    });
                });

            return mockHandler;
        }

        /// <summary>
        /// Cria um mock de ILogEntry com valores padrão configuráveis.
        /// </summary>
        private Mock<ILogEntry> CreateMockLogEntry(
    object state = null,
    string message = "Test message",
    LogLevel logLevel = LogLevel.Information)
        {
            var mockEntry = new Mock<ILogEntry>();

            // ✅ Não substitui null - usa valor passado
            mockEntry.Setup(e => e.State).Returns(state);
            mockEntry.Setup(e => e.Message).Returns(message);
            mockEntry.Setup(e => e.LogLevel).Returns(logLevel);
            mockEntry.Setup(e => e.Timestamp).Returns(DateTimeOffset.UtcNow);
            mockEntry.Setup(e => e.Category).Returns("TestCategory");
            mockEntry.Setup(e => e.EventId).Returns(new EventId(1, "TestEvent"));
            mockEntry.Setup(e => e.Exception).Returns((Exception)null);
            mockEntry.Setup(e => e.Scopes).Returns(new Dictionary<string, object>());

            return mockEntry;
        }

        #endregion
    }
}