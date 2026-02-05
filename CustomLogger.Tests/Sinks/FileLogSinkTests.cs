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
    public sealed class FileLogSinkTests : IDisposable
    {
        // ✅ Diretório temporário para testes
        private readonly string _testDir;

        public FileLogSinkTests()
        {
            _testDir = Path.Combine(Path.GetTempPath(), $"customlogger-tests-{Guid.NewGuid()}");
            Directory.CreateDirectory(_testDir);
        }

        // ────────────────────────────────────────
        // Criação de Arquivo
        // ────────────────────────────────────────

        // ✅ Teste 1: Arquivo é criado automaticamente
        [Fact]
        public void Write_ArquivoNaoExiste_CriaArquivo()
        {
            var path = Path.Combine(_testDir, "teste1.log");
            var sink = new FileLogSink(path, new JsonLogFormatter());

            sink.Write(CriarEntry("Log 1"));
            sink.Dispose();

            Assert.True(File.Exists(path));
        }

        // ✅ Teste 2: Pasta não existe → cria pasta e arquivo
        [Fact]
        public void Write_PastaNaoExiste_CriaPastaEArquivo()
        {
            var path = Path.Combine(_testDir, "subpasta", "nested", "teste2.log");
            var sink = new FileLogSink(path, new JsonLogFormatter());

            sink.Write(CriarEntry("Log 1"));
            sink.Dispose();

            Assert.True(File.Exists(path));
        }

        // ────────────────────────────────────────
        // Escrita
        // ────────────────────────────────────────

        // ✅ Teste 3: Log é escrito no arquivo
        [Fact]
        public void Write_LogEscritoNoArquivo()
        {
            var path = Path.Combine(_testDir, "teste3.log");
            var sink = new FileLogSink(path, new JsonLogFormatter());

            sink.Write(CriarEntry("Mensagem no arquivo"));
            sink.Dispose();

            var content = File.ReadAllText(path);
            Assert.Contains("Mensagem no arquivo", content);
        }

        // ✅ Teste 4: Múltiplos logs → cada um em linha separada
        [Fact]
        public void Write_MultiplosLogs_LinhasSeparadas()
        {
            var path = Path.Combine(_testDir, "teste4.log");
            var sink = new FileLogSink(path, new JsonLogFormatter());

            for (int i = 0; i < 5; i++)
            {
                sink.Write(CriarEntry($"Log {i}"));
            }
            sink.Dispose();

            var lines = File.ReadAllLines(path)
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToArray();

            Assert.Equal(5, lines.Length);
        }

        // ✅ Teste 5: Append mode → não sobrescreve
        [Fact]
        public void Write_AppendMode_NaoSobrescreve()
        {
            var path = Path.Combine(_testDir, "teste5.log");

            // Primeiro sink
            using (var sink1 = new FileLogSink(path, new JsonLogFormatter(), append: true))
            {
                sink1.Write(CriarEntry("Log anterior"));
            }

            // Segundo sink (mesmo arquivo)
            using (var sink2 = new FileLogSink(path, new JsonLogFormatter(), append: true))
            {
                sink2.Write(CriarEntry("Log novo"));
            }

            var content = File.ReadAllText(path);
            Assert.Contains("Log anterior", content);
            Assert.Contains("Log novo", content);
        }

        // ✅ Teste 6: WriteBatch → todos escritos
        [Fact]
        public void WriteBatch_TodosEscritos()
        {
            var path = Path.Combine(_testDir, "teste6.log");
            var sink = new FileLogSink(path, new JsonLogFormatter());

            var entries = Enumerable.Range(0, 5)
                .Select(i => (ILogEntry)CriarEntry($"Batch {i}"))
                .ToList();

            sink.WriteBatch(entries);
            sink.Dispose();

            var lines = File.ReadAllLines(path)
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToArray();

            Assert.Equal(5, lines.Length);
        }

        // ────────────────────────────────────────
        // Resiliência
        // ────────────────────────────────────────

        // ✅ Teste 7: Entry nula → não falha
        [Fact]
        public void Write_EntryNula_NaoFalha()
        {
            var path = Path.Combine(_testDir, "teste7.log");
            var sink = new FileLogSink(path, new JsonLogFormatter());

            sink.Write(null);
            sink.Dispose();
        }

        // ✅ Teste 8: WriteBatch nulo → não falha
        [Fact]
        public void WriteBatch_Nulo_NaoFalha()
        {
            var path = Path.Combine(_testDir, "teste8.log");
            var sink = new FileLogSink(path, new JsonLogFormatter());

            sink.WriteBatch(null);
            sink.Dispose();
        }

        // ✅ Teste 9: Dispose após dispose → não falha
        [Fact]
        public void Dispose_Duplo_NaoFalha()
        {
            var path = Path.Combine(_testDir, "teste9.log");
            var sink = new FileLogSink(path, new JsonLogFormatter());

            sink.Write(CriarEntry("Log 1"));

            sink.Dispose();
            sink.Dispose();
        }

        // ✅ Teste 10: Write após dispose → não falha
        [Fact]
        public void Write_AposDispose_NaoFalha()
        {
            var path = Path.Combine(_testDir, "teste10.log");
            var sink = new FileLogSink(path, new JsonLogFormatter());

            sink.Dispose();
            sink.Write(CriarEntry("Log após dispose"));  // Deve ser ignorado
        }

        // ✅ Teste 11: Path inválido → lança no construtor
        [Fact]
        public void Construtor_PathInvalido_LancaExcecao()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new FileLogSink("", new JsonLogFormatter());
            });
        }

        // ✅ Teste 12: Formatter nulo → lança no construtor
        [Fact]
        public void Construtor_FormatterNulo_LancaExcecao()
        {
            var path = Path.Combine(_testDir, "teste12.log");

            Assert.Throws<ArgumentNullException>(() =>
            {
                new FileLogSink(path, null);
            });
        }

        // ────────────────────────────────────────
        // IDisposable (cleanup de testes)
        // ────────────────────────────────────────
        public void Dispose()
        {
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, recursive: true);
            }
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
