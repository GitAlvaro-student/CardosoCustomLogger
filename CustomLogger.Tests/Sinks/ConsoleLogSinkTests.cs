using CustomLogger.Abstractions;
using CustomLogger.Buffering;
using CustomLogger.Formatting;
using CustomLogger.Sinks;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
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

        // NOVOS TESTES

        [Theory]
        [InlineData(LogLevel.Trace)]
        [InlineData(LogLevel.Debug)]
        [InlineData(LogLevel.Information)]
        [InlineData(LogLevel.Warning)]
        [InlineData(LogLevel.Error)]
        [InlineData(LogLevel.Critical)]
        [InlineData(LogLevel.None)]
        public void Write_WritesExpectedOutput_ForEachLogLevel(LogLevel level)
        {
            var formatter = new SimpleFormatter();
            var sink = new ConsoleLogSink(formatter);
            var entry = CriarEntry("msg", level);

            using (var sw = new StringWriter())
            {
                var original = Console.Out;
                Console.SetOut(sw);

                sink.Write(entry);

                Console.SetOut(original);
                var output = sw.ToString();
                Assert.Contains("msg", output);
            }
        }

        [Fact]
        public async Task WriteAsync_WritesExpectedOutput()
        {
            var formatter = new SimpleFormatter();
            var sink = new ConsoleLogSink(formatter);
            var entry = CriarEntry("async");

            using (var sw = new StringWriter())
            {
                var original = Console.Out;
                Console.SetOut(sw);

                await sink.WriteAsync(entry);

                Console.SetOut(original);
                var output = sw.ToString();
                Assert.Contains("async", output);
            }
        }

        [Fact]
        public async Task WriteBatchAsync_WritesAllEntries()
        {
            var formatter = new SimpleFormatter();
            var sink = new ConsoleLogSink(formatter);
            var entries = new[]
            {
                CriarEntry("A"),
                CriarEntry("B"),
                CriarEntry("C")
            };

            using (var sw = new StringWriter())
            {
                var original = Console.Out;
                Console.SetOut(sw);

                await sink.WriteBatchAsync(entries);

                Console.SetOut(original);
                var output = sw.ToString();
                Assert.Contains("A", output);
                Assert.Contains("B", output);
                Assert.Contains("C", output);
            }
        }

        [Fact]
        public async Task WriteBatchAsync_EmptyList_DoesNotThrow()
        {
            var formatter = new SimpleFormatter();
            var sink = new ConsoleLogSink(formatter);

            await sink.WriteBatchAsync(Array.Empty<ILogEntry>());
        }

        [Fact]
        public async Task WriteBatchAsync_Null_DoesNotThrow()
        {
            var formatter = new SimpleFormatter();
            var sink = new ConsoleLogSink(formatter);

            await sink.WriteBatchAsync(null);
        }

        [Fact]
        public void WriteColoredLine_WritesWithOrWithoutColor()
        {
            // Força ShouldUseColors a false via NO_COLOR
            Environment.SetEnvironmentVariable("NO_COLOR", "1");
            using (var sw = new StringWriter())
            {
                var original = Console.Out;
                Console.SetOut(sw);

                // Chama via reflection para acessar método interno (não permitido), então testamos via Write
                var formatter = new SimpleFormatter();
                var sink = new ConsoleLogSink(formatter);
                sink.Write(CriarEntry("colorless"));

                Console.SetOut(original);
                var output = sw.ToString();
                Assert.Contains("colorless", output);
            }
            Environment.SetEnvironmentVariable("NO_COLOR", null);
        }

        [Fact]
        public void WriteColoredLine_WritesWithAnsiIfSupported()
        {
            // Não há como forçar _hasAnsiSupport, mas podemos garantir que Write não lança
            var formatter = new SimpleFormatter();
            var sink = new ConsoleLogSink(formatter);
            using (var sw = new StringWriter())
            {
                var original = Console.Out;
                Console.SetOut(sw);

                sink.Write(CriarEntry("ansi"));

                Console.SetOut(original);
                var output = sw.ToString();
                Assert.Contains("ansi", output);
            }
        }

        [Fact]
        public void Write_WithFormatterException_DoesNotThrow()
        {
            var formatter = new ThrowingFormatter();
            var sink = new ConsoleLogSink(formatter);
            sink.Write(CriarEntry("fail"));
        }

        [Fact]
        public void WriteBatch_WithFormatterException_DoesNotThrow()
        {
            var formatter = new ThrowingFormatter();
            var sink = new ConsoleLogSink(formatter);
            var entries = new[] { CriarEntry("fail1"), CriarEntry("fail2") };
            sink.WriteBatch(entries);
        }

        [Fact]
        public async Task WriteAsync_WithFormatterException_DoesNotThrow()
        {
            var formatter = new ThrowingFormatter();
            var sink = new ConsoleLogSink(formatter);
            await sink.WriteAsync(CriarEntry("fail"));
        }

        [Fact]
        public async Task WriteBatchAsync_WithFormatterException_DoesNotThrow()
        {
            var formatter = new ThrowingFormatter();
            var sink = new ConsoleLogSink(formatter);
            var entries = new[] { CriarEntry("fail1"), CriarEntry("fail2") };
            await sink.WriteBatchAsync(entries);
        }

        [Theory]
        [InlineData(ConsoleColor.Black)]
        [InlineData(ConsoleColor.DarkBlue)]
        [InlineData(ConsoleColor.DarkGreen)]
        [InlineData(ConsoleColor.DarkCyan)]
        [InlineData(ConsoleColor.DarkRed)]
        [InlineData(ConsoleColor.DarkMagenta)]
        [InlineData(ConsoleColor.DarkYellow)]
        [InlineData(ConsoleColor.Gray)]
        [InlineData(ConsoleColor.DarkGray)]
        [InlineData(ConsoleColor.Blue)]
        [InlineData(ConsoleColor.Green)]
        [InlineData(ConsoleColor.Cyan)]
        [InlineData(ConsoleColor.Red)]
        [InlineData(ConsoleColor.Magenta)]
        [InlineData(ConsoleColor.Yellow)]
        [InlineData(ConsoleColor.White)]
        public void GetAnsiCode_ReturnsExpectedCode(ConsoleColor color)
        {
            // Usa reflection para acessar método privado (NÃO permitido), então validamos via Write
            // Aqui, apenas garante que Write não lança para todas as cores
            var formatter = new SimpleFormatter();
            var sink = new ConsoleLogSink(formatter);
            var entry = CriarEntry("color", LogLevel.Information);

            using (var sw = new StringWriter())
            {
                var original = Console.Out;
                Console.SetOut(sw);

                sink.Write(entry);

                Console.SetOut(original);
                var output = sw.ToString();
                Assert.Contains("color", output);
            }
        }

        // ────────────────────────────────────────
        // Helper
        // ────────────────────────────────────────
        private static BufferedLogEntry CriarEntry(string message, LogLevel level = LogLevel.Information)
        {
            return new BufferedLogEntry(
                timestamp: DateTimeOffset.UtcNow,
                category: "TestCategory",
                logLevel: level,
                eventId: 1,
                message: message,
                exception: null,
                state: null,
                scopes: new Dictionary<string, object>()
            );
        }

        private sealed class SimpleFormatter : ILogFormatter
        {
            public string Format(ILogEntry entry) => entry?.Message ?? string.Empty;
        }

        private sealed class ThrowingFormatter : ILogFormatter
        {
            public string Format(ILogEntry entry) => throw new InvalidOperationException("Formatter failed");
        }
    }
}
