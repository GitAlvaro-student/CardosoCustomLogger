using CustomLogger.Abstractions;
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
    /// <summary>
    /// Testes que validam invariantes de fallback entre sinks.
    /// 
    /// NOTA: Estes são exemplos de testes - não código de produção.
    /// Demonstram como validar conformidade com RFC.
    /// </summary>
    public class CompositeLogSinkFallbackTests
    {
        #region Test Doubles (Mocks)

        /// <summary>
        /// Sink de teste que sempre tem sucesso.
        /// </summary>
        private class SuccessfulSink : ILogSink
        {
            public List<ILogEntry> ReceivedEntries { get; } = new List<ILogEntry>();

            public void Write(ILogEntry entry)
            {
                ReceivedEntries.Add(entry);
            }
        }

        /// <summary>
        /// Sink de teste que sempre falha.
        /// </summary>
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

        /// <summary>
        /// Sink de teste que rastreia ordem de chamadas.
        /// </summary>
        private class OrderTrackingSink : ILogSink
        {
            public static int _globalCallOrder = 0;
            public int CallOrder { get; private set; }

            public void Write(ILogEntry entry)
            {
                CallOrder = ++_globalCallOrder;
            }
        }

        /// <summary>
        /// Log entry de teste simples.
        /// </summary>
        private class TestLogEntry : ILogEntry
        {
            public DateTimeOffset Timestamp { get; set; }

            public string Category { get; set; }

            public LogLevel LogLevel { get; set; }

            public EventId EventId { get; set; }

            public string Message { get; set; }

            public Exception Exception { get; set; }

            public object State { get; set; }

            public IReadOnlyDictionary<string, object> Scopes { get; }

            public string TraceId { get; }

            public string SpanId { get; }

            public string ParentSpanId { get; }

            public string ServiceName { get; }

            public string Environment { get; }
        }

        #endregion

        #region Invariante #1: Ordem de Tentativa É Imutável

        [Fact]
        public void Invariant1_SinksAreTriedInFixedOrder()
        {
            // Arrange
            var sink1 = new OrderTrackingSink();
            var sink2 = new OrderTrackingSink();
            var sink3 = new OrderTrackingSink();

            var composite = new CompositeLogSink(new[] { sink1, sink2, sink3 });
            var entry = new TestLogEntry { Message = "test" };

            // Act
            composite.Write(entry);

            // Assert - RFC: Ordem FIXA
            Assert.Equal(1, sink1.CallOrder);
            Assert.Equal(2, sink2.CallOrder);
            Assert.Equal(3, sink3.CallOrder);
        }

        [Fact]
        public void Invariant1_OrderRemainsConsistentAcrossMultipleFlushes()
        {
            // Arrange
            var sink1 = new OrderTrackingSink();
            var sink2 = new OrderTrackingSink();

            var composite = new CompositeLogSink(new[] { sink1, sink2 });

            // Act - Flush #1
            OrderTrackingSink._globalCallOrder = 0; // Reset
            composite.Write(new TestLogEntry());
            int flush1_sink1 = sink1.CallOrder;
            int flush1_sink2 = sink2.CallOrder;

            // Act - Flush #2
            OrderTrackingSink._globalCallOrder = 0; // Reset
            composite.Write(new TestLogEntry());
            int flush2_sink1 = sink1.CallOrder;
            int flush2_sink2 = sink2.CallOrder;

            // Assert - RFC: Mesma ordem em ambos os flushes
            Assert.Equal(flush1_sink1, flush2_sink1); // Sink1 sempre primeiro
            Assert.Equal(flush1_sink2, flush2_sink2); // Sink2 sempre segundo
        }

        #endregion

        #region Invariante #2: Cada Sink Tentado NO MÁXIMO Uma Vez

        [Fact]
        public void Invariant2_EachSinkAttemptedOnlyOnce()
        {
            // Arrange
            var failingSink = new FailingSink();
            var composite = new CompositeLogSink(new[] { failingSink });

            // Act
            composite.Write(new TestLogEntry());

            // Assert - RFC: Sink falhou mas foi tentado APENAS 1x (sem retry)
            Assert.Equal(1, failingSink.AttemptCount);
        }

        [Fact]
        public void Invariant2_NoRetryEvenOnCatastrophicFailure()
        {
            // Arrange
            var sink = new FailingSink
            {
                ExceptionToThrow = new OutOfMemoryException()
            };
            var composite = new CompositeLogSink(new[] { sink });

            // Act
            composite.Write(new TestLogEntry());

            // Assert - RFC: Mesmo com OutOfMemoryException, apenas 1 tentativa
            Assert.Equal(1, sink.AttemptCount);
        }

        #endregion

        #region Invariante #3: Falha de Um Sink NÃO Afeta Outros

        [Fact]
        public void Invariant3_FailureInFirstSinkDoesNotPreventSecondSink()
        {
            // Arrange
            var failingSink = new FailingSink();
            var successfulSink = new SuccessfulSink();

            var composite = new CompositeLogSink(new List<ILogSink> { failingSink, successfulSink });
            var entry = new TestLogEntry { Message = "test" };

            // Act
            composite.Write(entry);

            // Assert - RFC: Segundo sink foi tentado mesmo após falha do primeiro
            Assert.Equal(1, failingSink.AttemptCount); // Falhou
            Assert.Single(successfulSink.ReceivedEntries); // Sucesso
        }

        [Fact]
        public void Invariant3_MiddleSinkFailureDoesNotAffectSubsequentSinks()
        {
            // Arrange
            var sink1 = new SuccessfulSink();
            var sink2 = new FailingSink();
            var sink3 = new SuccessfulSink();

            var composite = new CompositeLogSink(new List<ILogSink> { sink1, sink2, sink3 });
            var entry = new TestLogEntry { Message = "test" };

            // Act
            composite.Write(entry);

            // Assert - RFC: Sink3 foi tentado mesmo após falha do Sink2
            Assert.Single(sink1.ReceivedEntries); // Sucesso
            Assert.Equal(1, sink2.AttemptCount);   // Falhou
            Assert.Single(sink3.ReceivedEntries);  // Sucesso
        }

        #endregion

        #region Invariante #4: Nenhuma Exceção Escapa do Composite

        [Fact]
        public void Invariant4_NoExceptionEscapesEvenIfAllSinksFail()
        {
            // Arrange
            var sink1 = new FailingSink { ExceptionToThrow = new IOException("Disk full") };
            var sink2 = new FailingSink { ExceptionToThrow = new TimeoutException("Network timeout") };
            var sink3 = new FailingSink { ExceptionToThrow = new InvalidOperationException("General failure") };

            var composite = new CompositeLogSink(new[] { sink1, sink2, sink3 });

            // Act & Assert - RFC: Nenhuma exceção lançada
            var exception = Record.Exception(() => composite.Write(new TestLogEntry()));

            Assert.Null(exception); // NENHUMA exceção escapou
        }

        [Fact]
        public void Invariant4_NoExceptionEvenWithNullEntry()
        {
            // Arrange
            var composite = new CompositeLogSink(new[] { new SuccessfulSink() });

            // Act & Assert - RFC: Guard rail - entrada nula é no-op sem exceção
            var exception = Record.Exception(() => composite.Write(null));

            Assert.Null(exception);
        }

        #endregion

        #region Invariante #5: Sem Loops Infinitos

        [Fact]
        public void Invariant5_ProcessingTerminatesWithFixedNumberOfSinks()
        {
            // Arrange
            int sinkCount = 10;
            var failingSinks = new List<FailingSink>();

            for (int i = 0; i < sinkCount; i++)
            {
                failingSinks.Add(new FailingSink());
            }

            var composite = new CompositeLogSink(failingSinks);

            // Act
            composite.Write(new TestLogEntry());

            // Assert - RFC: Cada sink tentado EXATAMENTE uma vez (10 iterações)
            foreach (var sink in failingSinks)
            {
                Assert.Equal(1, sink.AttemptCount);
            }

            // RFC: Total de tentativas = número de sinks (sem retry, sem loop infinito)
            int totalAttempts = failingSinks.Sum(s => s.AttemptCount);
            Assert.Equal(sinkCount, totalAttempts);
        }

        #endregion

        #region Invariante #6: Batch É Materializado UMA VEZ

        [Fact]
        public void Invariant6_BatchIsMaterializedOnce()
        {
            // Arrange
            int enumerationCount = 0;

            // IEnumerable que rastreia quantas vezes foi iterado
            IEnumerable<ILogEntry> CreateBatch()
            {
                enumerationCount++;
                yield return new TestLogEntry { Message = "log1" };
                yield return new TestLogEntry { Message = "log2" };
            }

            var sink1 = new SuccessfulSink();
            var sink2 = new SuccessfulSink();

            var composite = new CompositeLogSink(new[] { sink1, sink2 });

            // Act
            composite.WriteBatch(CreateBatch());

            // Assert - RFC: Batch iterado APENAS 1x (materializado no início)
            Assert.Equal(1, enumerationCount);

            // Ambos os sinks receberam os mesmos 2 logs
            Assert.Equal(2, sink1.ReceivedEntries.Count);
            Assert.Equal(2, sink2.ReceivedEntries.Count);
        }

        #endregion

        #region Guard Rails

        [Fact]
        public void GuardRail_EmptySinkListIsNoOp()
        {
            // Arrange
            var composite = new CompositeLogSink(new ILogSink[0]);

            // Act & Assert - RFC: Sem sinks é no-op sem exceção
            var exception = Record.Exception(() => composite.Write(new TestLogEntry()));

            Assert.Null(exception);
        }

        [Fact]
        public void GuardRail_NullBatchIsNoOp()
        {
            // Arrange
            var composite = new CompositeLogSink(new[] { new SuccessfulSink() });

            // Act & Assert - RFC: Batch nulo é no-op sem exceção
            var exception = Record.Exception(() => composite.WriteBatch(null));

            Assert.Null(exception);
        }

        [Fact]
        public void GuardRail_EmptyBatchIsNoOp()
        {
            // Arrange
            var sink = new SuccessfulSink();
            var composite = new CompositeLogSink(new[] { sink });

            // Act
            composite.WriteBatch(new List<ILogEntry>());

            // Assert - RFC: Batch vazio é no-op (sink não foi chamado)
            Assert.Empty(sink.ReceivedEntries);
        }

        #endregion

        #region Cenários Complexos (Integração)

        [Fact]
        public void Scenario_PartialSuccess_SomeSinksFailSomeSucceed()
        {
            // Arrange - Cenário: 5 sinks, alguns falham, alguns sucedem
            var sink1 = new SuccessfulSink();
            var sink2 = new FailingSink();
            var sink3 = new SuccessfulSink();
            var sink4 = new FailingSink();
            var sink5 = new SuccessfulSink();

            var composite = new CompositeLogSink(new ILogSink[]
            {
                sink1, sink2, sink3, sink4, sink5
            });

            var entry = new TestLogEntry { Message = "test" };

            // Act
            composite.Write(entry);

            // Assert - RFC: Sinks saudáveis receberam log
            Assert.Single(sink1.ReceivedEntries);
            Assert.Single(sink3.ReceivedEntries);
            Assert.Single(sink5.ReceivedEntries);

            // Assert - RFC: Sinks falhados foram tentados
            Assert.Equal(1, sink2.AttemptCount);
            Assert.Equal(1, sink4.AttemptCount);

            // Assert - RFC: Nenhuma exceção escapou
            // (teste passou = nenhuma exceção foi lançada)
        }

        [Fact]
        public void Scenario_AllSinksFail_NoExceptionAndNoRetry()
        {
            // Arrange - Cenário: TODOS os sinks falham
            var sink1 = new FailingSink { ExceptionToThrow = new IOException() };
            var sink2 = new FailingSink { ExceptionToThrow = new TimeoutException() };
            var sink3 = new FailingSink { ExceptionToThrow = new InvalidOperationException() };

            var composite = new CompositeLogSink(new[] { sink1, sink2, sink3 });

            // Act
            composite.Write(new TestLogEntry());

            // Assert - RFC: Cada sink tentado 1x (sem retry)
            Assert.Equal(1, sink1.AttemptCount);
            Assert.Equal(1, sink2.AttemptCount);
            Assert.Equal(1, sink3.AttemptCount);

            // Assert - RFC: Nenhuma exceção escapou (teste passou)
        }

        #endregion
    }
}
