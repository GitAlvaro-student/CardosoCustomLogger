using CustomLogger.Abstractions;
using CustomLogger.Buffering;
using CustomLogger.Formatting;
using CustomLogger.Sinks;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomLogger.Tests.Sinks
{
    public sealed class ConsoleLogSinkTests
    {
        // ────────────────────────────────────────
        // Write
        // ────────────────────────────────────────

        // ✅ Teste 1: Recebe ILogEntry sem falhar
        [Fact]
        public void Write_EntryValida_NaoFalha()
        {
            var formatter = new JsonLogFormatter();
            var sink = new ConsoleLogSink(formatter);

            // Não deve lançar exceção
            sink.Write(CriarEntry("Log console teste"));
        }

        // ✅ Teste 2: Entry nula → não falha
        [Fact]
        public void Write_EntryNula_NaoFalha()
        {
            var formatter = new JsonLogFormatter();
            var sink = new ConsoleLogSink(formatter);

            sink.Write(null);
        }

        // ✅ Teste 3: Formatação JSON válida
        [Fact]
        public void Write_FormatacaoJSON_Valida()
        {
            var formatter = new JsonLogFormatter();
            var entry = CriarEntry("Teste JSON");

            var json = formatter.Format(entry);

            // Deve ser JSON válido
            Assert.Contains("\"message\":\"Teste JSON\"", json);
            Assert.Contains("\"level\":\"Information\"", json);
            Assert.Contains("\"category\":\"TestCategory\"", json);
        }

        // ✅ Teste 4: WriteBatch sem falhar
        [Fact]
        public void WriteBatch_EntriasValidas_NaoFalha()
        {
            var formatter = new JsonLogFormatter();
            var sink = new ConsoleLogSink(formatter);

            var entries = new[]
            {
            CriarEntry("Log 1"),
            CriarEntry("Log 2"),
            CriarEntry("Log 3")
        };

            sink.WriteBatch(entries);
        }

        // ✅ Teste 5: WriteBatch com lista vazia → não falha
        [Fact]
        public void WriteBatch_ListaVazia_NaoFalha()
        {
            var formatter = new JsonLogFormatter();
            var sink = new ConsoleLogSink(formatter);

            sink.WriteBatch(Enumerable.Empty<ILogEntry>());
        }

        // ✅ Teste 6: WriteBatch nulo → não falha
        [Fact]
        public void WriteBatch_Nulo_NaoFalha()
        {
            var formatter = new JsonLogFormatter();
            var sink = new ConsoleLogSink(formatter);

            sink.WriteBatch(null);
        }

        // ✅ Teste 7: Dispose não falha
        [Fact]
        public void Dispose_NaoFalha()
        {
            var formatter = new JsonLogFormatter();
            var sink = new ConsoleLogSink(formatter);

            sink.Dispose();
            sink.Dispose();  // Duplo dispose
        }

        // ────────────────────────────────────────
        // Helper
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
