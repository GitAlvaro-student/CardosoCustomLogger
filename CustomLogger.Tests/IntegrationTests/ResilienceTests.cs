using CustomLogger.Buffering;
using CustomLogger.Configurations;
using CustomLogger.Providers;
using CustomLogger.Tests.Mocks;
using CustomLogger.Tests.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomLogger.Tests.IntegrationTests
{
    public sealed class ResilienciaTests
    {
        // ────────────────────────────────────────
        // Sink lança exceção
        // ────────────────────────────────────────

        // ✅ Teste 1: Sink falha → app não quebra
        [Fact]
        public void Sink_Falha_AppNaoQuebra()
        {
            var provider = new CustomLoggerProviderBuilder()
                .WithOptions(opts =>
                {
                    opts.MinimumLogLevel = LogLevel.Trace;
                    opts.UseGlobalBuffer = false;
                })
                .AddSink(new FailingSink())
                .Build();

            var logger = provider.CreateLogger("Test");

            // Nenhuma exceção deve escapar
            logger.LogTrace("Trace");
            logger.LogDebug("Debug");
            logger.LogInformation("Info");
            logger.LogWarning("Warning");
            logger.LogError("Error");
            logger.LogCritical("Critical");

            provider.Dispose();
        }

        // ✅ Teste 2: Sink falha após N escritas → logs anteriores preservados
        [Fact]
        public void Sink_FalhaAposN_LogsAnterioPreservados()
        {
            var sink = new FailAfterNSink(failAfter: 3);

            var provider = new CustomLoggerProviderBuilder()
                .WithOptions(opts =>
                {
                    opts.MinimumLogLevel = LogLevel.Trace;
                    opts.UseGlobalBuffer = false;
                })
                .AddSink(sink)
                .Build();

            var logger = provider.CreateLogger("Test");

            for (int i = 0; i < 10; i++)
            {
                logger.LogInformation($"Log {i}");
            }

            provider.Dispose();

            // Apenas os 3 primeiros foram escritos
            Assert.Equal(3, sink.WrittenEntries.Count);
        }

        // ✅ Teste 3: Sink falha → outro sink recebe
        [Fact]
        public void Sink_Falha_OutroSinkRecebe()
        {
            var failingSink = new FailingSink();
            var mockSink = new MockLogSink();

            var provider = new CustomLoggerProviderBuilder()
                .WithOptions(opts =>
                {
                    opts.MinimumLogLevel = LogLevel.Trace;
                    opts.UseGlobalBuffer = false;
                })
                .AddSink(failingSink)
                .AddSink(mockSink)
                .Build();

            var logger = provider.CreateLogger("Test");

            for (int i = 0; i < 5; i++)
            {
                logger.LogInformation($"Log {i}");
            }

            provider.Dispose();

            Assert.Equal(5, mockSink.WrittenEntries.Count);
        }

        // ✅ Teste 4: Todos os sinks falham → silêncio
        [Fact]
        public void Sink_TodosFalham_Silencio()
        {
            var provider = new CustomLoggerProviderBuilder()
                .WithOptions(opts =>
                {
                    opts.MinimumLogLevel = LogLevel.Trace;
                    opts.UseGlobalBuffer = false;
                })
                .AddSink(new FailingSink())
                .AddSink(new FailingSink())
                .AddSink(new FailingSink())
                .Build();

            var logger = provider.CreateLogger("Test");

            // Nenhuma exceção
            for (int i = 0; i < 100; i++)
            {
                logger.LogInformation($"Log {i}");
            }

            provider.Dispose();
        }

        // ────────────────────────────────────────
        // Async falha
        // ────────────────────────────────────────

        [Fact]
        public void Async_Falha_AppNaoQuebra()
        {
            var provider = new CustomLoggerProviderBuilder()
                .WithOptions(opts =>
                {
                    opts.MinimumLogLevel = LogLevel.Trace;
                    opts.UseGlobalBuffer = true;
                    opts.BatchOptions = new BatchOptions
                    {
                        BatchSize = 5,
                        FlushIntervalMs = 0
                    };
                })
                .AddSink(new FailingAsyncSink())
                .Build();

            var logger = provider.CreateLogger("Test");

            for (int i = 0; i < 10; i++)
            {
                logger.LogInformation($"Log {i}");
            }

            // Dispose já chama Flush internamente
            // Nenhuma exceção deve escapar
            provider.Dispose();
        }

        // ✅ Teste 6: Async falha → sink síncrono recebe
        [Fact]
        public async Task Async_Falha_SinkSincronoRecebe()
        {
            var mockSink = new MockLogSink();

            var provider = new CustomLoggerProviderBuilder()
                .WithOptions(opts =>
                {
                    opts.MinimumLogLevel = LogLevel.Trace;
                    opts.UseGlobalBuffer = true;
                    opts.BatchOptions = new BatchOptions
                    {
                        BatchSize = 5,
                        FlushIntervalMs = 0
                    };
                })
                .AddSink(new FailingAsyncSink())
                .AddSink(mockSink)
                .Build();

            var logger = provider.CreateLogger("Test");

            for (int i = 0; i < 5; i++)
            {
                logger.LogInformation($"Log {i}");
            }

            provider.Dispose();

            Assert.Equal(5, mockSink.WrittenEntries.Count);
        }

        // ────────────────────────────────────────
        // Buffer estoura
        // ────────────────────────────────────────

        // ✅ Teste 7: Buffer com alta carga → não falha
        [Fact]
        public void Buffer_AltaCarga_NaoFalha()
        {
            var mockSink = new MockLogSink();

            var provider = new CustomLoggerProviderBuilder()
                .WithOptions(opts =>
                {
                    opts.MinimumLogLevel = LogLevel.Trace;
                    opts.UseGlobalBuffer = true;
                    opts.BatchOptions = new BatchOptions
                    {
                        BatchSize = 100,
                        FlushIntervalMs = 0
                    };
                })
                .AddSink(mockSink)
                .Build();

            var logger = provider.CreateLogger("Test");

            // 10.000 logs sem exceção
            for (int i = 0; i < 10000; i++)
            {
                logger.LogInformation($"Log {i}");
            }

            provider.Dispose();

            Assert.Equal(10000, mockSink.WrittenEntries.Count);
        }

        // ✅ Teste 8: Buffer com threads paralelas → não falha
        [Fact]
        public async Task Buffer_ThreadsParalelas_NaoFalha()
        {
            var mockSink = new MockLogSink();

            var provider = new CustomLoggerProviderBuilder()
                .WithOptions(opts =>
                {
                    opts.MinimumLogLevel = LogLevel.Trace;
                    opts.UseGlobalBuffer = true;
                    opts.BatchOptions = new BatchOptions
                    {
                        BatchSize = 50,
                        FlushIntervalMs = 0
                    };
                })
                .AddSink(mockSink)
                .Build();

            var tasks = Enumerable.Range(0, 1000).Select(i =>
                Task.Run(() =>
                {
                    var logger = provider.CreateLogger($"Cat{i % 5}");
                    logger.LogInformation($"Log {i}");
                })
            ).ToArray();

            await Task.WhenAll(tasks);
            provider.Dispose();

            Assert.Equal(1000, mockSink.WrittenEntries.Count);
        }

        // ✅ Teste 9: Buffer com sink lento → não bloqueia
        [Fact]
        public void Buffer_SinkLento_NaoBloqueiaEnqueue()
        {
            var slowSink = new SlowSink(delayMs: 200);

            var provider = new CustomLoggerProviderBuilder()
                .WithOptions(opts =>
                {
                    opts.MinimumLogLevel = LogLevel.Trace;
                    opts.UseGlobalBuffer = true;
                    opts.BatchOptions = new BatchOptions
                    {
                        BatchSize = 100,  // Não faz flush até Dispose
                        FlushIntervalMs = 0
                    };
                })
                .AddSink(slowSink)
                .Build();

            var logger = provider.CreateLogger("Test");
            var stopwatch = Stopwatch.StartNew();

            // Enqueue deve ser rápido (não espera sink)
            for (int i = 0; i < 10; i++)
            {
                logger.LogInformation($"Log {i}");
            }

            stopwatch.Stop();

            // Enqueue não deve levar mais que 100ms (sink é 200ms por write)
            Assert.True(stopwatch.ElapsedMilliseconds < 100);

            provider.Dispose();
        }

        // ────────────────────────────────────────
        // Dispose abruptu
        // ────────────────────────────────────────

        // ✅ Teste 10: Dispose durante escrita → não falha
        [Fact]
        public async Task Dispose_DuranteEscrita_NaoFalha()
        {
            var slowSink = new SlowSink(delayMs: 100);

            var provider = new CustomLoggerProviderBuilder()
                .WithOptions(opts =>
                {
                    opts.MinimumLogLevel = LogLevel.Trace;
                    opts.UseGlobalBuffer = true;
                    opts.BatchOptions = new BatchOptions
                    {
                        BatchSize = 5,
                        FlushIntervalMs = 0
                    };
                })
                .AddSink(slowSink)
                .Build();

            var logger = provider.CreateLogger("Test");

            // Inicia escrita em background
            var writeTask = Task.Run(() =>
            {
                for (int i = 0; i < 100; i++)
                {
                    logger.LogInformation($"Log {i}");
                }
            });

            // Dispose enquanto ainda escreve
            await Task.Delay(50);
            provider.Dispose();  // Não deve lançar exceção

            await writeTask;
        }

        // ✅ Teste 11: Dispose com buffer cheio → flush final
        [Fact]
        public void Dispose_BufferCheio_FlushFinal()
        {
            var mockSink = new MockLogSink();

            var provider = new CustomLoggerProviderBuilder()
                .WithOptions(opts =>
                {
                    opts.MinimumLogLevel = LogLevel.Trace;
                    opts.UseGlobalBuffer = true;
                    opts.BatchOptions = new BatchOptions
                    {
                        BatchSize = 10000,  // Nunca faz flush automático
                        FlushIntervalMs = 0
                    };
                })
                .AddSink(mockSink)
                .Build();

            var logger = provider.CreateLogger("Test");

            for (int i = 0; i < 500; i++)
            {
                logger.LogInformation($"Log {i}");
            }

            Assert.Empty(mockSink.WrittenEntries);  // Buffer não flushou

            provider.Dispose();  // Flush final

            Assert.Equal(500, mockSink.WrittenEntries.Count);
        }

        // ✅ Teste 12: Dispose com todos os sinks falhos → não quebra
        [Fact]
        public void Dispose_TodosSinksFalhos_NaoQuebra()
        {
            var provider = new CustomLoggerProviderBuilder()
                .WithOptions(opts =>
                {
                    opts.MinimumLogLevel = LogLevel.Trace;
                    opts.UseGlobalBuffer = true;
                    opts.BatchOptions = new BatchOptions
                    {
                        BatchSize = 10000,
                        FlushIntervalMs = 0
                    };
                })
                .AddSink(new FailingSink())
                .AddSink(new FailingSink())
                .Build();

            var logger = provider.CreateLogger("Test");

            for (int i = 0; i < 100; i++)
            {
                logger.LogInformation($"Log {i}");
            }

            // Dispose com sinks falhos → não quebra
            provider.Dispose();
        }
    }
}
