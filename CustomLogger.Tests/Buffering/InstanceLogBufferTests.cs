using CustomLogger.Abstractions;
using CustomLogger.Buffering;
using CustomLogger.Configurations;
using CustomLogger.Tests.Models;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomLogger.Tests.Buffering
{
    public sealed class InstanceLogBufferTests
    {
        // ────────────────────────────────────────
        // Enqueue
        // ────────────────────────────────────────

        // ✅ Teste 1: Enqueue adiciona ao buffer
        [Fact]
        public void Enqueue_AdicionaAoBuffer()
        {
            var sink = new MockLogSink();
            var buffer = CriarBuffer(sink, useGlobalBuffer: true, batchSize: 100);

            buffer.Enqueue(CriarEntry("Log 1"));

            // Buffer não fez flush ainda (batchSize = 100)
            Assert.Empty(sink.WrittenEntries);
        }

        // ✅ Teste 2: Enqueue com entry nula → não adiciona
        [Fact]
        public void Enqueue_EntryNula_NaoAdiona()
        {
            var sink = new MockLogSink();
            var buffer = CriarBuffer(sink, useGlobalBuffer: true, batchSize: 100);

            buffer.Enqueue(null);
            buffer.Flush();

            Assert.Empty(sink.WrittenEntries);
        }

        // ✅ Teste 3: Múltiplos enqueues acumulam no buffer
        [Fact]
        public void Enqueue_Multiplos_AcumulaNoBuffer()
        {
            var sink = new MockLogSink();
            var buffer = CriarBuffer(sink, useGlobalBuffer: true, batchSize: 100);

            for (int i = 0; i < 5; i++)
            {
                buffer.Enqueue(CriarEntry($"Log {i}"));
            }

            // Nada foi escrito ainda
            Assert.Empty(sink.WrittenEntries);

            buffer.Flush();

            // Agora todos foram escritos
            Assert.Equal(5, sink.WrittenEntries.Count);
        }

        // ────────────────────────────────────────
        // Flush
        // ────────────────────────────────────────

        // ✅ Teste 4: Flush esvazia o buffer
        [Fact]
        public void Flush_EvaziaBuffer()
        {
            var sink = new MockLogSink();
            var buffer = CriarBuffer(sink, useGlobalBuffer: true, batchSize: 100);

            buffer.Enqueue(CriarEntry("Log 1"));
            buffer.Enqueue(CriarEntry("Log 2"));
            buffer.Flush();

            Assert.Equal(2, sink.WrittenEntries.Count);
        }

        // ✅ Teste 5: Flush em buffer vazio → não falha
        [Fact]
        public void Flush_BufferVazio_NaoFalha()
        {
            var sink = new MockLogSink();
            var buffer = CriarBuffer(sink, useGlobalBuffer: true, batchSize: 100);

            // Não deve lançar exceção
            buffer.Flush();

            Assert.Empty(sink.WrittenEntries);
        }

        // ✅ Teste 6: Flush duplo → não duplica logs
        [Fact]
        public void Flush_Duplo_NaoDuplicaLogs()
        {
            var sink = new MockLogSink();
            var buffer = CriarBuffer(sink, useGlobalBuffer: true, batchSize: 100);

            buffer.Enqueue(CriarEntry("Log 1"));

            buffer.Flush();
            buffer.Flush();  // Segundo flush não deve escrever nada

            Assert.Single(sink.WrittenEntries);
        }

        // ✅ Teste 7: Flush automático por BatchSize
        [Fact]
        public void Flush_AutomaticoPorBatchSize()
        {
            var sink = new MockLogSink();
            var buffer = CriarBuffer(sink, useGlobalBuffer: true, batchSize: 3);

            buffer.Enqueue(CriarEntry("Log 0"));
            buffer.Enqueue(CriarEntry("Log 1"));
            Assert.Empty(sink.WrittenEntries);  // Ainda não fez flush

            buffer.Enqueue(CriarEntry("Log 2"));  // Atinge BatchSize → flush automático

            Assert.Equal(3, sink.WrittenEntries.Count);
        }

        // ────────────────────────────────────────
        // Dispose
        // ────────────────────────────────────────

        // ✅ Teste 8: Dispose força flush
        [Fact]
        public void Dispose_ForcaFlush()
        {
            var sink = new MockLogSink();
            var buffer = CriarBuffer(sink, useGlobalBuffer: true, batchSize: 100);

            buffer.Enqueue(CriarEntry("Log 1"));
            buffer.Enqueue(CriarEntry("Log 2"));

            // Nada escrito ainda
            Assert.Empty(sink.WrittenEntries);

            buffer.Dispose();

            // Dispose forçou flush
            Assert.Equal(2, sink.WrittenEntries.Count);
        }

        // ✅ Teste 9: Dispose duplo → não falha
        [Fact]
        public void Dispose_Duplo_NaoFalha()
        {
            var sink = new MockLogSink();
            var buffer = CriarBuffer(sink, useGlobalBuffer: true, batchSize: 100);

            buffer.Enqueue(CriarEntry("Log 1"));

            buffer.Dispose();
            buffer.Dispose();  // Não deve lançar exceção

            Assert.Single(sink.WrittenEntries);
        }

        // ✅ Teste 10: Enqueue após Dispose → ignorado
        [Fact]
        public void Enqueue_AposDispose_Ignorado()
        {
            var sink = new MockLogSink();
            var buffer = CriarBuffer(sink, useGlobalBuffer: true, batchSize: 100);

            buffer.Dispose();
            buffer.Enqueue(CriarEntry("Log após dispose"));

            Assert.Empty(sink.WrittenEntries);
        }

        // ────────────────────────────────────────
        // UseGlobalBuffer = false
        // ────────────────────────────────────────

        // ✅ Teste 11: UseGlobalBuffer false → escrita imediata
        [Fact]
        public void Enqueue_UseGlobalBufferFalse_EscritoImediatamente()
        {
            var sink = new MockLogSink();
            var buffer = CriarBuffer(sink, useGlobalBuffer: false);

            buffer.Enqueue(CriarEntry("Log imediato"));

            // Escrito imediatamente, sem flush
            Assert.Single(sink.WrittenEntries);
            Assert.Equal("Log imediato", sink.WrittenEntries[0].Message);
        }

        // ✅ Teste 12: UseGlobalBuffer false → múltiplos logs escritos na ordem
        [Fact]
        public void Enqueue_UseGlobalBufferFalse_PreservaOrdem()
        {
            var sink = new MockLogSink();
            var buffer = CriarBuffer(sink, useGlobalBuffer: false);

            for (int i = 0; i < 5; i++)
            {
                buffer.Enqueue(CriarEntry($"Log {i}"));
            }

            Assert.Equal(5, sink.WrittenEntries.Count);

            for (int i = 0; i < 5; i++)
            {
                Assert.Equal($"Log {i}", sink.WrittenEntries[i].Message);
            }
        }

        // ✅ Teste 13: UseGlobalBuffer false → Flush não falha (buffer vazio)
        [Fact]
        public void Flush_UseGlobalBufferFalse_NaoFalha()
        {
            var sink = new MockLogSink();
            var buffer = CriarBuffer(sink, useGlobalBuffer: false);

            buffer.Enqueue(CriarEntry("Log 1"));
            buffer.Flush();  // Buffer já está vazio

            Assert.Single(sink.WrittenEntries);
        }

        // ────────────────────────────────────────
        // Resiliência
        // ────────────────────────────────────────

        // ✅ Teste 14: Sink falha no Flush → não lança exceção
        [Fact]
        public void Flush_SinkFalha_NaoLancaExcecao()
        {
            var sink = new FailingSink();
            var buffer = CriarBuffer(sink, useGlobalBuffer: true, batchSize: 100);

            buffer.Enqueue(CriarEntry("Log 1"));

            // Não deve lançar exceção
            buffer.Flush();
        }

        // ✅ Teste 15: Sink falha na escrita imediata → não lança exceção
        [Fact]
        public void Enqueue_UseGlobalBufferFalse_SinkFalha_NaoLancaExcecao()
        {
            var sink = new FailingSink();
            var buffer = CriarBuffer(sink, useGlobalBuffer: false);

            // Não deve lançar exceção
            buffer.Enqueue(CriarEntry("Log 1"));
        }

        // ────────────────────────────────────────
        // Helpers
        // ────────────────────────────────────────
        private static InstanceLogBuffer CriarBuffer(
            ILogSink sink,
            bool useGlobalBuffer = true,
            int batchSize = 100,
            TimeSpan? flushInterval = null)
        {
            var options = new CustomProviderOptions
            {
                UseGlobalBuffer = useGlobalBuffer,
                BatchOptions = new BatchOptions
                {
                    BatchSize = batchSize,
                    FlushInterval = flushInterval ?? TimeSpan.Zero
                }
            };

            return new InstanceLogBuffer(sink, options);
        }

        private static BufferedLogEntry CriarEntry(string message)
        {
            return new BufferedLogEntry(
                timestamp: DateTimeOffset.UtcNow,
                category: "Test",
                logLevel: Microsoft.Extensions.Logging.LogLevel.Information,
                eventId: 1,
                message: message,
                exception: null,
                state: null,
                scopes: new Dictionary<string, object>()
            );
        }
    }
}