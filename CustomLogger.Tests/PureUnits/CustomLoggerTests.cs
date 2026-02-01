using CustomLogger.Configurations;
using CustomLogger.Tests.Mocks;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomLogger.Tests.PureUnits
{
    public sealed class CustomLoggerTests
    {
        // ────────────────────────────────────────
        // IsEnabled
        // ────────────────────────────────────────

        // ✅ Teste 1: LogLevel abaixo do mínimo → não habilitado
        [Theory]
        [InlineData(LogLevel.Trace, LogLevel.Debug)]
        [InlineData(LogLevel.Trace, LogLevel.Information)]
        [InlineData(LogLevel.Debug, LogLevel.Information)]
        [InlineData(LogLevel.Information, LogLevel.Warning)]
        [InlineData(LogLevel.Warning, LogLevel.Error)]
        [InlineData(LogLevel.Error, LogLevel.Critical)]
        public void IsEnabled_LogLevel_AbaixoDoMinimo_RetornaFalse(LogLevel atual, LogLevel minimo)
        {
            var logger = CriarLogger(minimumLogLevel: minimo);

            Assert.False(logger.IsEnabled(atual));
        }

        // ✅ Teste 2: LogLevel igual ao mínimo → habilitado
        [Theory]
        [InlineData(LogLevel.Trace)]
        [InlineData(LogLevel.Debug)]
        [InlineData(LogLevel.Information)]
        [InlineData(LogLevel.Warning)]
        [InlineData(LogLevel.Error)]
        [InlineData(LogLevel.Critical)]
        public void IsEnabled_LogLevel_IgualAoMinimo_RetornaTrue(LogLevel nivel)
        {
            var logger = CriarLogger(minimumLogLevel: nivel);

            Assert.True(logger.IsEnabled(nivel));
        }

        // ✅ Teste 3: LogLevel.None nunca é habilitado
        [Theory]
        [InlineData(LogLevel.Trace)]
        [InlineData(LogLevel.Debug)]
        [InlineData(LogLevel.Information)]
        [InlineData(LogLevel.Warning)]
        [InlineData(LogLevel.Error)]
        [InlineData(LogLevel.Critical)]
        [InlineData(LogLevel.None)]
        public void IsEnabled_LogLevelNone_SempreRetornaFalse(LogLevel minimo)
        {
            var logger = CriarLogger(minimumLogLevel: minimo);

            Assert.False(logger.IsEnabled(LogLevel.None));
        }

        // ────────────────────────────────────────
        // Log() - Enqueue
        // ────────────────────────────────────────

        // ✅ Teste 4: LogLevel habilitado → Enqueue chamado
        [Fact]
        public void Log_LogLevelHabilitado_EnqueueChamado()
        {
            var buffer = new MockLogBuffer();
            var logger = CriarLogger(buffer: buffer, minimumLogLevel: LogLevel.Trace);

            logger.LogTrace("Mensagem trace");

            Assert.Single(buffer.EnqueuedEntries);
        }

        // ✅ Teste 5: LogLevel não habilitado → Enqueue não chamado
        [Fact]
        public void Log_LogLevelNaoHabilitado_EnqueueNaoChamado()
        {
            var buffer = new MockLogBuffer();
            var logger = CriarLogger(buffer: buffer, minimumLogLevel: LogLevel.Information);

            logger.LogDebug("Mensagem debug");

            Assert.Empty(buffer.EnqueuedEntries);
        }

        // ✅ Teste 6: LogLevel.None → Enqueue não chamado
        [Fact]
        public void Log_LogLevelNone_EnqueueNaoChamado()
        {
            var buffer = new MockLogBuffer();
            var logger = CriarLogger(buffer: buffer, minimumLogLevel: LogLevel.Trace);

            logger.Log(LogLevel.None, "Mensagem none");

            Assert.Empty(buffer.EnqueuedEntries);
        }

        // ────────────────────────────────────────
        // Log() - BufferedLogEntry
        // ────────────────────────────────────────

        // ✅ Teste 7: Entry preserva mensagem correta
        [Fact]
        public void Log_PreservaMensagem()
        {
            var buffer = new MockLogBuffer();
            var logger = CriarLogger(buffer: buffer, minimumLogLevel: LogLevel.Trace);

            logger.LogInformation("Mensagem de teste");

            var entry = buffer.EnqueuedEntries.Single();
            Assert.Equal("Mensagem de teste", entry.Message);
        }

        // ✅ Teste 8: Entry preserva LogLevel
        [Theory]
        [InlineData(LogLevel.Trace)]
        [InlineData(LogLevel.Debug)]
        [InlineData(LogLevel.Information)]
        [InlineData(LogLevel.Warning)]
        [InlineData(LogLevel.Error)]
        [InlineData(LogLevel.Critical)]
        public void Log_PreservaLogLevel(LogLevel nivel)
        {
            var buffer = new MockLogBuffer();
            var logger = CriarLogger(buffer: buffer, minimumLogLevel: LogLevel.Trace);

            logger.Log(nivel, "Test");

            var entry = buffer.EnqueuedEntries.Single();
            Assert.Equal(nivel, entry.LogLevel);
        }

        // ✅ Teste 9: Entry preserva Category
        [Fact]
        public void Log_PreservaCategory()
        {
            var buffer = new MockLogBuffer();
            var logger = CriarLogger(buffer: buffer, category: "MinhaCategoria");

            logger.LogInformation("Test");

            var entry = buffer.EnqueuedEntries.Single();
            Assert.Equal("MinhaCategoria", entry.Category);
        }

        // ✅ Teste 10: Entry preserva Exception
        [Fact]
        public void Log_PreservaException()
        {
            var buffer = new MockLogBuffer();
            var logger = CriarLogger(buffer: buffer);
            var exception = new InvalidOperationException("Erro");

            logger.LogError(exception, "Erro ocorreu");

            var entry = buffer.EnqueuedEntries.Single();
            Assert.Equal(exception, entry.Exception);
        }

        // ✅ Teste 11: Entry sem exception → Exception nula
        [Fact]
        public void Log_SemException_ExceptionNula()
        {
            var buffer = new MockLogBuffer();
            var logger = CriarLogger(buffer: buffer);

            logger.LogInformation("Sem erro");

            var entry = buffer.EnqueuedEntries.Single();
            Assert.Null(entry.Exception);
        }

        // ✅ Teste 12: Entry preserva Timestamp
        [Fact]
        public void Log_PreservaTimestamp()
        {
            var buffer = new MockLogBuffer();
            var logger = CriarLogger(buffer: buffer);
            var antes = DateTimeOffset.UtcNow;

            logger.LogInformation("Test");

            var depois = DateTimeOffset.UtcNow;
            var entry = buffer.EnqueuedEntries.Single();

            Assert.True(entry.Timestamp >= antes);
            Assert.True(entry.Timestamp <= depois);
        }

        // ────────────────────────────────────────
        // Log() - Scopes
        // ────────────────────────────────────────

        // ✅ Teste 13: Scope ativo → preservado no entry
        [Fact]
        public void Log_ScopeAtivo_ScopePreservado()
        {
            var buffer = new MockLogBuffer();
            var scopes = new Dictionary<string, object>
            {
                ["RequestId"] = "abc-123",
                ["UserId"] = 42
            };
            var logger = CriarLogger(buffer: buffer, scopes: scopes);

            logger.LogInformation("Test");

            var entry = buffer.EnqueuedEntries.Single();
            Assert.Equal("abc-123", entry.Scopes["RequestId"]);
            Assert.Equal(42, entry.Scopes["UserId"]);
        }

        // ✅ Teste 14: Sem scope → Scopes vazio
        [Fact]
        public void Log_SemScope_ScopesVazio()
        {
            var buffer = new MockLogBuffer();
            var logger = CriarLogger(buffer: buffer);

            logger.LogInformation("Test");

            var entry = buffer.EnqueuedEntries.Single();
            Assert.Empty(entry.Scopes);
        }

        // ────────────────────────────────────────
        // Log() - Formatter null
        // ────────────────────────────────────────

        // ✅ Teste 15: Formatter null → lança exceção
        [Fact]
        public void Log_FormatterNull_LancaExcecao()
        {
            var logger = CriarLogger();

            Assert.Throws<ArgumentNullException>(() =>
            {
                logger.Log(
                    LogLevel.Information,
                    1,
                    "state",
                    null,
                    null  // ← Formatter null
                );
            });
        }

        // ────────────────────────────────────────
        // Helper
        // ────────────────────────────────────────
        private static Loggers.CustomLogger CriarLogger(
            MockLogBuffer buffer = null,
            string category = "TestCategory",
            LogLevel minimumLogLevel = LogLevel.Information,
            IReadOnlyDictionary<string, object> scopes = null)
        {
            buffer ??= new MockLogBuffer();

            var options = new CustomProviderOptions
            {
                MinimumLogLevel = minimumLogLevel
            };

            var configuration = new CustomProviderConfiguration(options);
            var scopeProvider = new MockLogScopeProvider(scopes);

            return new Loggers.CustomLogger(category, configuration, buffer, scopeProvider);
        }
    }
}
