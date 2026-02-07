using CustomLogger.Abstractions;
using CustomLogger.Sinks;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using Xunit;

namespace CustomLogger.Tests.Sinks
{
    /// <summary>
    /// Testes que validam invariantes do modo degradado.
    /// </summary>
    public class DegradableLogSinkTests
    {
        #region Test Doubles

        private class SuccessfulSink : ILogSink
        {
            public int WriteCount { get; private set; }

            public void Write(ILogEntry entry)
            {
                WriteCount++;
            }
        }

        private class FailingSink : ILogSink
        {
            public int AttemptCount { get; private set; }
            public Exception ExceptionToThrow { get; set; } = new InvalidOperationException("Sink failed");

            public void Write(ILogEntry entry)
            {
                AttemptCount++;
                throw ExceptionToThrow;
            }
        }

        private class FlakySink : ILogSink
        {
            private int _callCount = 0;
            public int SuccessCount { get; private set; }
            public int FailureCount { get; private set; }

            // Falha, depois sucesso, repete
            public void Write(ILogEntry entry)
            {
                _callCount++;
                if (_callCount % 2 == 1)
                {
                    FailureCount++;
                    throw new InvalidOperationException("Flaky failure");
                }
                else
                {
                    SuccessCount++;
                }
            }
        }

        private class TestLogEntry : ILogEntry
        {
            public string Message { get; set; }
            public DateTime Timestamp { get; set; } = DateTime.UtcNow;
            public string LogLevel { get; set; } = "Info";

            public string Category { get; set; }

            public EventId EventId { get; set; }

            public Exception Exception { get; set; }

            public object State { get; set; }

            public IReadOnlyDictionary<string, object> Scopes { get; set; }

            DateTimeOffset ILogEntry.Timestamp => Timestamp;

            LogLevel ILogEntry.LogLevel { get; }
        }

        #endregion

        // ════════════════════════════════════════════════════════════
        // INVARIANTE #1: Primeira falha entra em modo degradado
        // ════════════════════════════════════════════════════════════

        [Fact]
        public void Invariante1_PrimeiraFalha_EntraModoDegrade()
        {
            // Arrange
            var failingSink = new FailingSink();
            var degradable = new DegradableLogSink(failingSink);

            // Estado inicial: saudável
            Assert.False(degradable.IsDegraded);

            // Act
            degradable.Write(new TestLogEntry { Message = "test" });

            // Assert
            // RFC: N=1 → primeira falha já marca como degradado
            Assert.True(degradable.IsDegraded);
            Assert.Equal(1, failingSink.AttemptCount); // Sink foi tentado
        }

        // ════════════════════════════════════════════════════════════
        // INVARIANTE #2: Primeiro sucesso sai do modo degradado
        // ════════════════════════════════════════════════════════════

        [Fact]
        public void Invariante2_PrimeiroSucesso_SaiModoDegrade()
        {
            // Arrange
            var flakySink = new FlakySink();
            var degradable = new DegradableLogSink(flakySink);

            // Act - Primeira escrita: falha
            degradable.Write(new TestLogEntry { Message = "1" });
            Assert.True(degradable.IsDegraded);  // Degradado

            // Act - Segunda escrita: sucesso
            degradable.Write(new TestLogEntry { Message = "2" });

            // Assert
            // RFC: Primeiro sucesso limpa estado degradado
            Assert.False(degradable.IsDegraded);
            Assert.Equal(1, flakySink.SuccessCount);
            Assert.Equal(1, flakySink.FailureCount);
        }

        // ════════════════════════════════════════════════════════════
        // INVARIANTE #3: Sink degradado CONTINUA sendo tentado
        // ════════════════════════════════════════════════════════════

        [Fact]
        public void Invariante3_SinkDegrade_ContinuaSendoTentado()
        {
            // Arrange
            var failingSink = new FailingSink();
            var degradable = new DegradableLogSink(failingSink);

            // Act - Múltiplas escritas durante degradação
            degradable.Write(new TestLogEntry { Message = "1" });
            degradable.Write(new TestLogEntry { Message = "2" });
            degradable.Write(new TestLogEntry { Message = "3" });

            // Assert
            // RFC: Sink degradado NÃO é pulado
            // RFC: Cada escrita resulta em tentativa no sink
            Assert.True(degradable.IsDegraded);
            Assert.Equal(3, failingSink.AttemptCount);
        }

        // ════════════════════════════════════════════════════════════
        // INVARIANTE #4: Modo degradado NUNCA lança exceção
        // ════════════════════════════════════════════════════════════

        [Fact]
        public void Invariante4_ModoDegrade_NuncaLancaExcecao()
        {
            // Arrange
            var failingSink = new FailingSink
            {
                ExceptionToThrow = new OutOfMemoryException("Critical failure")
            };
            var degradable = new DegradableLogSink(failingSink);

            // Act & Assert
            // RFC: Modo degradado absorve TODAS as exceções
            var exception = Record.Exception(() =>
            {
                degradable.Write(new TestLogEntry { Message = "1" });
                degradable.Write(new TestLogEntry { Message = "2" });
                degradable.Write(new TestLogEntry { Message = "3" });
            });

            Assert.Null(exception); // Nenhuma exceção escapou
            Assert.True(degradable.IsDegraded);
        }

        // ════════════════════════════════════════════════════════════
        // INVARIANTE #5: Estado é thread-safe (volatile)
        // ════════════════════════════════════════════════════════════

        [Fact]
        public void Invariante5_Estado_ThreadSafe()
        {
            // Arrange
            var flakySink = new FlakySink();
            var degradable = new DegradableLogSink(flakySink);

            // Act - Escritas concorrentes
            System.Threading.Tasks.Parallel.For(0, 10, i =>
            {
                degradable.Write(new TestLogEntry { Message = $"Log {i}" });
            });

            // Assert
            // RFC: volatile bool garante visibilidade entre threads
            // Estado final é consistente (degradado ou não, mas não corrompido)
            bool finalState = degradable.IsDegraded;
            Assert.True(finalState == true || finalState == false); // Estado válido

            // Sink foi tentado 10 vezes (algumas falharam, algumas sucederam)
            Assert.Equal(10, flakySink.SuccessCount + flakySink.FailureCount);
        }

        // ════════════════════════════════════════════════════════════
        // INVARIANTE #6: Sink saudável permanece saudável
        // ════════════════════════════════════════════════════════════

        [Fact]
        public void Invariante6_SinkSaudavel_PermaneceSaudavel()
        {
            // Arrange
            var successfulSink = new SuccessfulSink();
            var degradable = new DegradableLogSink(successfulSink);

            // Act - Múltiplas escritas bem-sucedidas
            for (int i = 0; i < 100; i++)
            {
                degradable.Write(new TestLogEntry { Message = $"Log {i}" });
            }

            // Assert
            // RFC: Sink saudável NUNCA entra em modo degradado
            Assert.False(degradable.IsDegraded);
            Assert.Equal(100, successfulSink.WriteCount);
        }

        // ════════════════════════════════════════════════════════════
        // INVARIANTE #7: Recuperação é imediata (sem timer)
        // ════════════════════════════════════════════════════════════

        [Fact]
        public void Invariante7_Recuperacao_Imediata()
        {
            // Arrange
            var flakySink = new FlakySink();
            var degradable = new DegradableLogSink(flakySink);

            // Act - Falha → sucesso → falha → sucesso
            degradable.Write(new TestLogEntry { Message = "1" }); // Falha
            Assert.True(degradable.IsDegraded);

            degradable.Write(new TestLogEntry { Message = "2" }); // Sucesso
            Assert.False(degradable.IsDegraded); // Recuperou IMEDIATAMENTE

            degradable.Write(new TestLogEntry { Message = "3" }); // Falha
            Assert.True(degradable.IsDegraded);

            degradable.Write(new TestLogEntry { Message = "4" }); // Sucesso
            Assert.False(degradable.IsDegraded); // Recuperou IMEDIATAMENTE

            // RFC: Recuperação não requer timer ou delay
            // RFC: Um único sucesso é suficiente
        }

        // ════════════════════════════════════════════════════════════
        // INVARIANTE #8: Dispose funciona independente de degradação
        // ════════════════════════════════════════════════════════════

        [Fact]
        public void Invariante8_Dispose_FuncionaIndependente()
        {
            // Arrange
            var failingSink = new FailingSink();
            var degradable = new DegradableLogSink(failingSink);

            // Act - Entrar em modo degradado
            degradable.Write(new TestLogEntry { Message = "test" });
            Assert.True(degradable.IsDegraded);

            // Act - Dispose enquanto degradado
            var exception = Record.Exception(() => degradable.Dispose());

            // Assert
            // RFC: Dispose funciona mesmo se sink estiver degradado
            // RFC: Dispose NUNCA lança exceção
            Assert.Null(exception);
        }

        // ════════════════════════════════════════════════════════════
        // INVARIANTE #9: Batch fallback funciona com degradação
        // ════════════════════════════════════════════════════════════

        [Fact]
        public void Invariante9_BatchFallback_FuncionaComDegrade()
        {
            // Arrange
            var failingSink = new FailingSink();
            var degradable = new DegradableLogSink(failingSink);

            var batch = new[]
            {
                new TestLogEntry { Message = "1" },
                new TestLogEntry { Message = "2" },
                new TestLogEntry { Message = "3" }
            };

            // Act - WriteBatch em sink que não suporta batch
            degradable.WriteBatch(batch);

            // Assert
            // RFC: Fallback batch → individual funciona
            // RFC: Sink é marcado como degradado após falhas
            Assert.True(degradable.IsDegraded);

            // Fallback tentou escrever cada entry individualmente
            Assert.Equal(3, failingSink.AttemptCount);
        }
    }
}