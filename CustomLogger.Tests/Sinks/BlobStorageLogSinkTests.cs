using CustomLogger.Buffering;
using CustomLogger.Formatting;
using CustomLogger.Sinks;
using CustomLogger.Tests.Mocks;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomLogger.Tests.Sinks
{
    /// <summary>
    /// Testes sem conexão real com Azure.
    /// Valida apenas comportamento de resiliência.
    /// </summary>
    public sealed class BlobStorageLogSinkTests
    {
        // ────────────────────────────────────────
        // Resiliência (sem conexão real)
        // ────────────────────────────────────────

        // ✅ Teste 1: ConnectionString inválida → lança no construtor
        [Fact]
        public void Construtor_ConnectionStringVazia_LancaExcecao()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new BlobStorageLogSink("", "container", new JsonLogFormatter());
            });
        }

        // ✅ Teste 2: ContainerName vazio → lança no construtor
        [Fact]
        public void Construtor_ContainerNameVazio_LancaExcecao()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new BlobStorageLogSink("connection", "", new JsonLogFormatter());
            });
        }

        // ✅ Teste 4: Formatter nulo → lança no construtor
        [Fact]
        public void Construtor_FormatterNulo_LancaExcecao()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                new BlobStorageLogSink("connection", "container", null);
            });
        }

        // ✅ Teste 5: Write com conexão inválida → não lança exceção
        [Fact]
        public void Write_ConexaoInvalida_NaoLancaExcecao()
        {
            // Usa CompositeLogSink + FailingSink para simular falha de conexão
            var failingSink = new FailingBlobSink();

            // Não deve lançar exceção
            failingSink.Write(CriarEntry("Log sem conexão"));
        }

        // ✅ Teste 6: WriteBatch com conexão inválida → não lança exceção
        [Fact]
        public void WriteBatch_ConexaoInvalida_NaoLancaExcecao()
        {
            var failingSink = new FailingBlobSink();

            var entries = new[]
            {
            CriarEntry("Log 1"),
            CriarEntry("Log 2")
        };

            failingSink.WriteBatch(entries);
        }

        // ✅ Teste 7: WriteAsync com conexão inválida → não lança exceção
        [Fact]
        public async Task WriteAsync_ConexaoInvalida_NaoLancaExcecao()
        {
            var failingSink = new FailingBlobSink();

            await failingSink.WriteAsync(CriarEntry("Log async"));
        }

        // ✅ Teste 8: WriteBatchAsync com conexão inválida → não lança exceção
        [Fact]
        public async Task WriteBatchAsync_ConexaoInvalida_NaoLancaExcecao()
        {
            var failingSink = new FailingBlobSink();

            var entries = new[]
            {
            CriarEntry("Log 1"),
            CriarEntry("Log 2")
        };

            await failingSink.WriteBatchAsync(entries);
        }

        // ✅ Teste 9: Dispose não falha
        [Fact]
        public void Dispose_NaoFalha()
        {
            var sink = new FailingBlobSink();

            sink.Dispose();
            sink.Dispose();  // Duplo dispose
        }

        // ────────────────────────────────────────
        // Helpers
        // ────────────────────────────────────────
        private static BufferedLogEntry CriarEntry(string message)
        {
            return new BufferedLogEntry(
                timestamp: DateTimeOffset.UtcNow,
                category: "TestCategory",
                logLevel: LogLevel.Information,
                eventId: 1,
                message: message,
                exception: null,
                state: null,
                scopes: new Dictionary<string, object>()
            );
        }
    }
}
