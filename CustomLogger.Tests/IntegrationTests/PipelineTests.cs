using CustomLogger.Configurations;
using CustomLogger.Providers;
using CustomLogger.Tests.Mocks;
using CustomLogger.Tests.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CustomLogger.Tests.IntegrationTests
{
    public sealed class PipelineTests
    {
        // ═══════════════════════════════════════════════════════════════
        // HELPER METHODS - Compatibilidade entre .NET Framework e .NET 8
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Lê arquivo com retry para contornar file locking em .NET Framework.
        /// </summary>
        private static string LerArquivoComRetry(string path, int maxRetries = 3, int delayMs = 50)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        return reader.ReadToEnd();
                    }
                }
                catch (IOException) when (i < maxRetries - 1)
                {
                    Thread.Sleep(delayMs);
                }
            }
            throw new IOException($"Não foi possível ler arquivo após {maxRetries} tentativas");
        }

        /// <summary>
        /// Parseia primeira linha JSON de arquivo NDJSON (uma linha por log).
        /// </summary>
        private static System.Text.Json.JsonDocument ParsearPrimeiraLinhaJSON(string path)
        {
            var content = LerArquivoComRetry(path);

            if (string.IsNullOrWhiteSpace(content))
                throw new InvalidOperationException($"Arquivo vazio: {path}");

            var lines = content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToArray();

            if (lines.Length == 0)
                throw new InvalidOperationException($"Nenhuma linha JSON válida em: {path}");

            return System.Text.Json.JsonDocument.Parse(lines[0]);
        }
        // ────────────────────────────────────────
        // Log simples chega ao destino
        // ────────────────────────────────────────

        // ✅ Teste 1: Log simples chega ao sink
        [Fact]
        public void Pipeline_LogSimples_ChegaAoSink()
        {
            var mockSink = new MockLogSink();

            var provider = new CustomLoggerProviderBuilder()
                .WithOptions(opts =>
                {
                    opts.MinimumLogLevel = LogLevel.Trace;
                    opts.UseGlobalBuffer = false;  // Escrita imediata
                })
                .AddSink(mockSink)
                .Build();

            var logger = provider.CreateLogger("TestCategory");
            logger.LogInformation("Log simples");

            provider.Dispose();

            Assert.Single(mockSink.WrittenEntries);
            Assert.Equal("Log simples", mockSink.WrittenEntries[0].Message);
            Assert.Equal("TestCategory", mockSink.WrittenEntries[0].Category);
            Assert.Equal(LogLevel.Information, mockSink.WrittenEntries[0].LogLevel);
        }

        // ✅ Teste 2: Log com exception chega completo
        [Fact]
        public void Pipeline_LogComException_ChegaCompleto()
        {
            var mockSink = new MockLogSink();

            var provider = new CustomLoggerProviderBuilder()
                .WithOptions(opts =>
                {
                    opts.MinimumLogLevel = LogLevel.Trace;
                    opts.UseGlobalBuffer = false;
                })
                .AddSink(mockSink)
                .Build();

            var logger = provider.CreateLogger("TestCategory");
            var exception = new InvalidOperationException("Erro de teste");

            logger.LogError(exception, "Erro ocorreu");
            provider.Dispose();

            var entry = mockSink.WrittenEntries.Single();
            Assert.Equal(exception, entry.Exception);
            Assert.Equal("Erro ocorreu", entry.Message);
            Assert.Equal(LogLevel.Error, entry.LogLevel);
        }

        // ✅ Teste 3: Log com scopes chega completo
        [Fact]
        public void Pipeline_LogComScopes_ChegaCompleto()
        {
            var mockSink = new MockLogSink();

            var provider = new CustomLoggerProviderBuilder()
                .WithOptions(opts =>
                {
                    opts.MinimumLogLevel = LogLevel.Trace;
                    opts.UseGlobalBuffer = false;
                })
                .AddSink(mockSink)
                .Build();

            var logger = provider.CreateLogger("TestCategory");

            using (logger.BeginScope(new Dictionary<string, object>
            {
                ["RequestId"] = "abc-123",
                ["UserId"] = 42
            }))
            {
                logger.LogInformation("Com scopes");
            }

            provider.Dispose();

            var entry = mockSink.WrittenEntries.Single();
            Assert.Equal("abc-123", entry.Scopes["RequestId"]);
            Assert.Equal(42, entry.Scopes["UserId"]);
        }

        // ────────────────────────────────────────
        // Batch
        // ────────────────────────────────────────

        // ✅ Teste 4: Logs em batch chegam completos
        [Fact]
        public void Pipeline_LogEmBatch_ChegaCompleto()
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
                .AddSink(mockSink)
                .Build();

            var logger = provider.CreateLogger("TestCategory");

            // Enviar exatamente BatchSize logs
            for (int i = 0; i < 5; i++)
            {
                logger.LogInformation($"Batch log {i}");
            }

            provider.Dispose();

            Assert.Equal(5, mockSink.WrittenEntries.Count);

            for (int i = 0; i < 5; i++)
            {
                Assert.Equal($"Batch log {i}", mockSink.WrittenEntries[i].Message);
            }
        }

        // ✅ Teste 5: Logs abaixo do BatchSize chegam no Dispose
        [Fact]
        public void Pipeline_LogsAbaixoBatchSize_ChegamNoDispose()
        {
            var mockSink = new MockLogSink();

            var provider = new CustomLoggerProviderBuilder()
                .WithOptions(opts =>
                {
                    opts.MinimumLogLevel = LogLevel.Trace;
                    opts.UseGlobalBuffer = true;
                    opts.BatchOptions = new BatchOptions
                    {
                        BatchSize = 100,  // Nunca atinge
                        FlushIntervalMs = 0
                    };
                })
                .AddSink(mockSink)
                .Build();

            var logger = provider.CreateLogger("TestCategory");

            logger.LogInformation("Log 1");
            logger.LogInformation("Log 2");
            logger.LogInformation("Log 3");

            // Antes do dispose → buffer não fez flush
            Assert.Empty(mockSink.WrittenEntries);

            provider.Dispose();

            // Dispose forçou flush
            Assert.Equal(3, mockSink.WrittenEntries.Count);
        }

        // ✅ Teste 6: Batch com logs de níveis diferentes
        [Fact]
        public void Pipeline_BatchNiveis_SoPreservaNiveis_Habilitados()
        {
            var mockSink = new MockLogSink();

            var provider = new CustomLoggerProviderBuilder()
                .WithOptions(opts =>
                {
                    opts.MinimumLogLevel = LogLevel.Warning;  // Só Warning+
                    opts.UseGlobalBuffer = false;
                })
                .AddSink(mockSink)
                .Build();

            var logger = provider.CreateLogger("TestCategory");

            logger.LogTrace("Trace");        // ❌ Ignorado
            logger.LogDebug("Debug");        // ❌ Ignorado
            logger.LogInformation("Info");   // ❌ Ignorado
            logger.LogWarning("Warning");    // ✅
            logger.LogError("Error");        // ✅
            logger.LogCritical("Critical");  // ✅

            provider.Dispose();

            Assert.Equal(3, mockSink.WrittenEntries.Count);
            Assert.Equal("Warning", mockSink.WrittenEntries[0].Message);
            Assert.Equal("Error", mockSink.WrittenEntries[1].Message);
            Assert.Equal("Critical", mockSink.WrittenEntries[2].Message);
        }

        // ────────────────────────────────────────
        // Dispose
        // ────────────────────────────────────────

        // ✅ Teste 7: Dispose final escreve tudo
        [Fact]
        public void Pipeline_DisposeFinal_EscreveTudo()
        {
            var mockSink = new MockLogSink();

            var provider = new CustomLoggerProviderBuilder()
                .WithOptions(opts =>
                {
                    opts.MinimumLogLevel = LogLevel.Trace;
                    opts.UseGlobalBuffer = true;
                    opts.BatchOptions = new BatchOptions
                    {
                        BatchSize = 1000,
                        FlushIntervalMs = 0
                    };
                })
                .AddSink(mockSink)
                .Build();

            var logger = provider.CreateLogger("TestCategory");

            for (int i = 0; i < 50; i++)
            {
                logger.LogInformation($"Log {i}");
            }

            Assert.Empty(mockSink.WrittenEntries);  // Buffer não flushou

            provider.Dispose();

            Assert.Equal(50, mockSink.WrittenEntries.Count);
        }

        // ✅ Teste 8: Dispose duplo → não falha
        [Fact]
        public void Pipeline_DisposeDuplo_NaoFalha()
        {
            var mockSink = new MockLogSink();

            var provider = new CustomLoggerProviderBuilder()
                .WithOptions(opts =>
                {
                    opts.MinimumLogLevel = LogLevel.Trace;
                    opts.UseGlobalBuffer = false;
                })
                .AddSink(mockSink)
                .Build();

            var logger = provider.CreateLogger("TestCategory");
            logger.LogInformation("Log 1");

            provider.Dispose();
            provider.Dispose();  // Não deve lançar exceção

            Assert.Single(mockSink.WrittenEntries);
        }

        // ✅ Teste 9: Log após Dispose → ignorado
        [Fact]
        public void Pipeline_LogAposDispose_Ignorado()
        {
            var mockSink = new MockLogSink();

            var provider = new CustomLoggerProviderBuilder()
                .WithOptions(opts =>
                {
                    opts.MinimumLogLevel = LogLevel.Trace;
                    opts.UseGlobalBuffer = false;
                })
                .AddSink(mockSink)
                .Build();

            var logger = provider.CreateLogger("TestCategory");
            logger.LogInformation("Antes do dispose");

            provider.Dispose();

            // Após dispose → deve ser ignorado (não falhar)
            logger.LogInformation("Após dispose");

            Assert.Single(mockSink.WrittenEntries);
        }

        // ────────────────────────────────────────
        // Múltiplos Sinks
        // ────────────────────────────────────────

        // ✅ Teste 10: Log chega em todos os sinks
        [Fact]
        public void Pipeline_MultiplosSinks_LogChegaEmTodos()
        {
            var sink1 = new MockLogSink();
            var sink2 = new MockLogSink();
            var sink3 = new MockLogSink();

            var provider = new CustomLoggerProviderBuilder()
                .WithOptions(opts =>
                {
                    opts.MinimumLogLevel = LogLevel.Trace;
                    opts.UseGlobalBuffer = false;
                })
                .AddSink(sink1)
                .AddSink(sink2)
                .AddSink(sink3)
                .Build();

            var logger = provider.CreateLogger("TestCategory");
            logger.LogInformation("Para todos");

            provider.Dispose();

            Assert.Single(sink1.WrittenEntries);
            Assert.Single(sink2.WrittenEntries);
            Assert.Single(sink3.WrittenEntries);
        }

        // ✅ Teste 11: Um sink falha → outros continuam
        [Fact]
        public void Pipeline_SinkFalha_OutrosContinuam()
        {
            var failingSink = new FailingSink();
            var mockSink = new MockLogSink();

            var provider = new CustomLoggerProviderBuilder()
                .WithOptions(opts =>
                {
                    opts.MinimumLogLevel = LogLevel.Trace;
                    opts.UseGlobalBuffer = false;
                })
                .AddSink(failingSink)  // Falha
                .AddSink(mockSink)     // Deve receber
                .Build();

            var logger = provider.CreateLogger("TestCategory");
            logger.LogInformation("Resiliente");

            provider.Dispose();

            Assert.Single(mockSink.WrittenEntries);
        }

        // ✅ Teste 12: Todos os sinks falham → não quebra aplicação
        [Fact]
        public void Pipeline_TodosSinksFalham_NaoQuebra()
        {
            var provider = new CustomLoggerProviderBuilder()
                .WithOptions(opts =>
                {
                    opts.MinimumLogLevel = LogLevel.Trace;
                    opts.UseGlobalBuffer = false;
                })
                .AddSink(new FailingSink())
                .AddSink(new FailingSink())
                .Build();

            var logger = provider.CreateLogger("TestCategory");

            // Nenhuma exceção deve escapar
            logger.LogInformation("Todos falham");
            logger.LogError("Erro também falha");

            provider.Dispose();
        }

        // ────────────────────────────────────────
        // Múltiplos Loggers Simultâneos
        // ────────────────────────────────────────

        // ✅ Teste 13: Múltiplos loggers → mesmo sink
        [Fact]
        public void Pipeline_MultiploLoggers_MesmoSink()
        {
            var mockSink = new MockLogSink();

            var provider = new CustomLoggerProviderBuilder()
                .WithOptions(opts =>
                {
                    opts.MinimumLogLevel = LogLevel.Trace;
                    opts.UseGlobalBuffer = false;
                })
                .AddSink(mockSink)
                .Build();

            var logger1 = provider.CreateLogger("Category1");
            var logger2 = provider.CreateLogger("Category2");
            var logger3 = provider.CreateLogger("Category3");

            logger1.LogInformation("Logger 1");
            logger2.LogInformation("Logger 2");
            logger3.LogInformation("Logger 3");

            provider.Dispose();

            Assert.Equal(3, mockSink.WrittenEntries.Count);
            Assert.Equal("Category1", mockSink.WrittenEntries[0].Category);
            Assert.Equal("Category2", mockSink.WrittenEntries[1].Category);
            Assert.Equal("Category3", mockSink.WrittenEntries[2].Category);
        }

        // ✅ Teste 14: Múltiplos loggers → scopes isolados
        [Fact]
        public void Pipeline_MultiploLoggers_ScopesIsolados()
        {
            var mockSink = new MockLogSink();

            var provider = new CustomLoggerProviderBuilder()
                .WithOptions(opts =>
                {
                    opts.MinimumLogLevel = LogLevel.Trace;
                    opts.UseGlobalBuffer = false;
                })
                .AddSink(mockSink)
                .Build();

            var logger1 = provider.CreateLogger("Logger1");
            var logger2 = provider.CreateLogger("Logger2");

            using (logger1.BeginScope(new Dictionary<string, object> { ["Source"] = "L1" }))
            {
                logger1.LogInformation("Msg L1");
                logger2.LogInformation("Msg L2");  // Não deve ter scope de L1
            }

            provider.Dispose();

            var entryL1 = mockSink.WrittenEntries.First(e => e.Message == "Msg L1");
            var entryL2 = mockSink.WrittenEntries.First(e => e.Message == "Msg L2");

            Assert.Equal("L1", entryL1.Scopes["Source"]);
            Assert.Empty(entryL2.Scopes);
        }

        // ✅ Teste 15: Múltiplos loggers simultâneos em threads paralelas
        [Fact]
        public async Task Pipeline_ThreadsParalelas_SemRaceCondition()
        {
            var mockSink = new MockLogSink();

            var provider = new CustomLoggerProviderBuilder()
                .WithOptions(opts =>
                {
                    opts.MinimumLogLevel = LogLevel.Trace;
                    opts.UseGlobalBuffer = false;
                })
                .AddSink(mockSink)
                .Build();

            var totalLogs = 1000;
            var tasks = Enumerable.Range(0, totalLogs).Select(i =>
                Task.Run(() =>
                {
                    var logger = provider.CreateLogger($"Category{i % 10}");
                    logger.LogInformation($"Log {i}");
                })
            ).ToArray();

            await Task.WhenAll(tasks);
            provider.Dispose();

            // Todos os 1000 logs devem ter chegado
            Assert.Equal(totalLogs, mockSink.WrittenEntries.Count);
        }

        // ────────────────────────────────────────
        // Formatter no pipeline
        // ────────────────────────────────────────

        // ✅ Teste 16: Formatter JSON no pipeline completo
        [Fact]
        public void Pipeline_FormatterJSON_Completo()
        {
            var path = Path.Combine(Path.GetTempPath(), $"pipeline-test-{Guid.NewGuid()}.log");

            try
            {
                var provider = new CustomLoggerProviderBuilder()
                    .WithOptions(opts =>
                    {
                        opts.MinimumLogLevel = LogLevel.Trace;
                        opts.UseGlobalBuffer = false;
                    })
                    .AddFileSink(path)
                    .Build();

                var logger = provider.CreateLogger("PipelineTest");

                using (logger.BeginScope(new Dictionary<string, object> { ["RequestId"] = "xyz-789" }))
                {
                    logger.LogWarning(new Exception("Erro"), "Teste completo");
                }

                provider.Dispose();

                using (var doc = ParsearPrimeiraLinhaJSON(path))
                {
                    var root = doc.RootElement;

                    Assert.Equal("Warning", root.GetProperty("level").GetString());
                    Assert.Equal("PipelineTest", root.GetProperty("category").GetString());
                    Assert.Equal("Teste completo", root.GetProperty("message").GetString());
                    Assert.NotEqual(System.Text.Json.JsonValueKind.Null, root.GetProperty("exception").ValueKind);
                    Assert.Equal("xyz-789", root.GetProperty("scopes").GetProperty("RequestId").GetString());
                }
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }
    }
}
