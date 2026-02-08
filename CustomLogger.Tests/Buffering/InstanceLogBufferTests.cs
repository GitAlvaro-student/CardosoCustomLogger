using CustomLogger.Abstractions;
using CustomLogger.Buffering;
using CustomLogger.Configurations;
using CustomLogger.Tests.Mocks;
using CustomLogger.Tests.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace CustomLogger.Tests.Buffering
{
    /// <summary>
    /// Testes unitários de InstanceLogBuffer.
    /// 
    /// CONTRATOS TESTADOS:
    /// - Enqueue nunca lança exceção (hot path resiliente)
    /// - Flush explícito entrega logs enfileirados
    /// - Dispose é idempotente e nunca lança exceção
    /// - Dispose não reativa buffer (Enqueue pós-Dispose é rejeitado)
    /// - UseGlobalBuffer=false → escrita imediata
    /// - Resiliência a falhas de sink
    /// 
    /// IMPORTANTE: Buffer.Dispose() NÃO faz flush implícito.
    /// Flush é responsabilidade do Provider ou deve ser chamado explicitamente.
    /// </summary>
    public sealed class InstanceLogBufferTests
    {
        // ────────────────────────────────────────
        // Enqueue
        // ────────────────────────────────────────

        // ✅ Teste 1: Enqueue adiciona ao buffer
        // CONTRATO: Logs são enfileirados, não escritos até Flush
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
        // CONTRATO: Guard rail - entrada nula é rejeitada silenciosamente
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
        // CONTRATO: Logs acumulam até Flush explícito
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

            // Flush explícito entrega todos os logs
            buffer.Flush();

            // Agora todos foram escritos
            Assert.Equal(5, sink.WrittenEntries.Count);
        }

        // ────────────────────────────────────────
        // Flush
        // ────────────────────────────────────────

        // ✅ Teste 4: Flush esvazia o buffer
        // CONTRATO: Flush explícito processa todos os logs enfileirados
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
        // CONTRATO: Flush é seguro mesmo sem logs pendentes
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
        // CONTRATO: Flush é idempotente (segundo flush não reprocessa logs)
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
        // CONTRATO: Batch size trigger faz flush automático
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

        // ✅ Teste 8 REFATORADO: Flush antes de Dispose preserva logs
        // CONTRATO ANTIGO (inválido): Dispose força flush automaticamente
        // CONTRATO ATUAL (válido): Flush explícito ANTES de Dispose preserva logs
        // 
        // IMPORTANTE: Buffer.Dispose() NÃO faz flush implícito.
        // Flush é responsabilidade do Provider (ou chamada explícita em testes).
        [Fact]
        public void Flush_AntesDeDispose_PreservaLogs()
        {
            var sink = new MockLogSink();
            var buffer = CriarBuffer(sink, useGlobalBuffer: true, batchSize: 100);

            buffer.Enqueue(CriarEntry("Log 1"));
            buffer.Enqueue(CriarEntry("Log 2"));

            // Nada escrito ainda
            Assert.Empty(sink.WrittenEntries);

            // FLUSH EXPLÍCITO (responsabilidade do caller, não do Dispose)
            buffer.Flush();

            // Agora Dispose é seguro (apenas para timer)
            buffer.Dispose();

            // Logs foram preservados pelo Flush ANTES do Dispose
            Assert.Equal(2, sink.WrittenEntries.Count);
        }

        // ✅ Teste 9 REFATORADO: Dispose duplo não lança exceção
        // CONTRATO: Dispose é idempotente e nunca lança exceção
        // 
        // MUDANÇA: Flush explícito adicionado para garantir logs escritos.
        // Teste valida idempotência + ausência de exceções, não flush implícito.
        [Fact]
        public void Dispose_Duplo_NaoLancaExcecao()
        {
            var sink = new MockLogSink();
            var buffer = CriarBuffer(sink, useGlobalBuffer: true, batchSize: 100);

            buffer.Enqueue(CriarEntry("Log 1"));

            // FLUSH EXPLÍCITO para garantir log escrito
            buffer.Flush();

            // Dispose duplo não deve lançar exceção
            buffer.Dispose();
            buffer.Dispose();  // Idempotência

            // Log foi escrito pelo Flush explícito (não pelo Dispose)
            Assert.Single(sink.WrittenEntries);
        }

        // ✅ Teste 10: Enqueue após Dispose → ignorado
        // CONTRATO: Dispose não reativa buffer (Enqueue pós-Dispose é rejeitado)
        [Fact]
        public void Enqueue_AposDispose_Ignorado()
        {
            var sink = new MockLogSink();
            var buffer = CriarBuffer(sink, useGlobalBuffer: true, batchSize: 100);

            buffer.Dispose();
            buffer.Enqueue(CriarEntry("Log após dispose"));

            // Enqueue após Dispose é rejeitado silenciosamente
            Assert.Empty(sink.WrittenEntries);
        }

        // ✅ Teste 11 NOVO: Dispose sem logs pendentes é seguro
        // CONTRATO: Dispose é seguro mesmo sem logs enfileirados
        [Fact]
        public void Dispose_SemLogsPendentes_NaoFalha()
        {
            var sink = new MockLogSink();
            var buffer = CriarBuffer(sink, useGlobalBuffer: true, batchSize: 100);

            // Dispose sem logs pendentes
            buffer.Dispose();

            // Não lança exceção, buffer está vazio
            Assert.Empty(sink.WrittenEntries);
        }

        // ────────────────────────────────────────
        // UseGlobalBuffer = false
        // ────────────────────────────────────────

        // ✅ Teste 12: UseGlobalBuffer false → escrita imediata
        // CONTRATO: Modo sem buffer escreve diretamente no sink
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

        // ✅ Teste 13: UseGlobalBuffer false → múltiplos logs escritos na ordem
        // CONTRATO: Modo sem buffer preserva ordem de escrita (FIFO)
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

        // ✅ Teste 14: UseGlobalBuffer false → Flush não falha (buffer vazio)
        // CONTRATO: Flush é no-op quando UseGlobalBuffer=false (buffer vazio)
        [Fact]
        public void Flush_UseGlobalBufferFalse_NaoFalha()
        {
            var sink = new MockLogSink();
            var buffer = CriarBuffer(sink, useGlobalBuffer: false);

            buffer.Enqueue(CriarEntry("Log 1"));
            buffer.Flush();  // Buffer já está vazio (escrita foi imediata)

            Assert.Single(sink.WrittenEntries);
        }

        // ────────────────────────────────────────
        // Resiliência
        // ────────────────────────────────────────

        // ✅ Teste 15: Sink falha no Flush → não lança exceção
        // CONTRATO: Falha de sink é absorvida (hot path resiliente)
        [Fact]
        public void Flush_SinkFalha_NaoLancaExcecao()
        {
            var sink = new FailingSink();
            var buffer = CriarBuffer(sink, useGlobalBuffer: true, batchSize: 100);

            buffer.Enqueue(CriarEntry("Log 1"));

            // Não deve lançar exceção (falha é absorvida)
            buffer.Flush();

            // Teste passa se não houve exceção
        }

        // ✅ Teste 16: Sink falha na escrita imediata → não lança exceção
        // CONTRATO: UseGlobalBuffer=false também absorve falhas de sink
        [Fact]
        public void Enqueue_UseGlobalBufferFalse_SinkFalha_NaoLancaExcecao()
        {
            var sink = new FailingSink();
            var buffer = CriarBuffer(sink, useGlobalBuffer: false);

            // Não deve lançar exceção (falha é absorvida)
            buffer.Enqueue(CriarEntry("Log 1"));

            // Teste passa se não houve exceção
        }

        // ✅ Teste 17: Dispose após falha de sink não lança exceção
        // CONTRATO: Dispose é resiliente mesmo com sink falhado
        [Fact]
        public void Dispose_AposFalhaDeSink_NaoLancaExcecao()
        {
            var sink = new FailingSink();
            var buffer = CriarBuffer(sink, useGlobalBuffer: true, batchSize: 100);

            buffer.Enqueue(CriarEntry("Log 1"));
            buffer.Flush();  // Sink falha, exceção é absorvida

            // Dispose não deve lançar exceção
            buffer.Dispose();

            // Teste passa se não houve exceção
        }

        // ────────────────────────────────────────
        // Helpers
        // ────────────────────────────────────────

        private static InstanceLogBuffer CriarBuffer(
            ILogSink sink,
            bool useGlobalBuffer = true,
            int batchSize = 100,
            int? flushInterval = null)
        {
            var options = new CustomProviderOptions
            {
                UseGlobalBuffer = useGlobalBuffer,
                BatchOptions = new BatchOptions
                {
                    BatchSize = batchSize,
                    FlushIntervalMs = flushInterval ?? 0
                }
            };

            return new InstanceLogBuffer(sink, options);
        }

        private static BufferedLogEntry CriarEntry(string message)
        {
            return new BufferedLogEntry(
                timestamp: DateTimeOffset.UtcNow,
                category: "Test",
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