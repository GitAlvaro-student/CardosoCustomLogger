using CustomLogger.Configurations;
using CustomLogger.Providers;
using CustomLogger.Tests.Mocks;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomLogger.Tests.Compatibility
{
    public sealed class RuntimeCompatibilityTests
    {
        // ────────────────────────────────────────
        // Thread vs Task
        // ────────────────────────────────────────

        // ✅ Teste 1: Thread.Sleep funciona em ambos
        [Fact]
        public void Thread_Sleep_FuncionaEmAmbos()
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

            var logger = provider.CreateLogger("Test");

            var thread = new Thread(() =>
            {
                Thread.Sleep(10);
                logger.LogInformation("Thread log");
            });

            thread.Start();
            thread.Join();

            provider.Dispose();

            Assert.Single(mockSink.WrittenEntries);
        }

        // ✅ Teste 2: Task.Run funciona em ambos
        [Fact]
        public async Task Task_Run_FuncionaEmAmbos()
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

            var logger = provider.CreateLogger("Test");

            await Task.Run(async () =>
            {
                await Task.Delay(10);
                logger.LogInformation("Task log");
            });

            provider.Dispose();

            Assert.Single(mockSink.WrittenEntries);
        }

        // ✅ Teste 3: AsyncLocal funciona em ambos
        [Fact]
        public async Task AsyncLocal_FuncionaEmAmbos()
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

            var logger = provider.CreateLogger("Test");

            using (logger.BeginScope(new Dictionary<string, object> { ["TaskId"] = "main" }))
            {
                await Task.Run(() =>
                {
                    logger.LogInformation("In task");
                });
            }

            provider.Dispose();

            var entry = mockSink.WrittenEntries.Single();
            Assert.Equal("main", entry.Scopes["TaskId"]);
        }

        // ✅ Teste 4: ConcurrentQueue funciona em ambos
        [Fact]
        public async Task ConcurrentQueue_FuncionaEmAmbos()
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
                        FlushInterval = TimeSpan.Zero
                    };
                })
                .AddSink(mockSink)
                .Build();

            var logger = provider.CreateLogger("Test");

            var tasks = Enumerable.Range(0, 50).Select(i =>
                Task.Run(() => logger.LogInformation($"Log {i}"))
            ).ToArray();

            await Task.WhenAll(tasks);
            provider.Dispose();

            Assert.Equal(50, mockSink.WrittenEntries.Count);
        }

        // ────────────────────────────────────────
        // Activity (Tracing)
        // ────────────────────────────────────────

        // ✅ Teste 5: Activity disponível em ambos
        [Fact]
        public void Activity_DisponivelEmAmbos()
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

            var logger = provider.CreateLogger("Test");

            using var activity = new Activity("TestOp").Start();
            var traceId = activity.TraceId.ToString();
            var spanId = activity.SpanId.ToString();

            using (logger.BeginScope(new Dictionary<string, object>
            {
                ["TraceId"] = traceId,
                ["SpanId"] = spanId
            }))
            {
                logger.LogInformation("With activity");
            }

            provider.Dispose();

            var entry = mockSink.WrittenEntries.Single();
            Assert.Equal(traceId, entry.Scopes["TraceId"]);
            Assert.Equal(spanId, entry.Scopes["SpanId"]);
            Assert.NotEmpty(traceId);
            Assert.NotEmpty(spanId);
        }

        // ────────────────────────────────────────
        // Encoding
        // ────────────────────────────────────────

        // ✅ Teste 6: UTF-8 funciona em ambos
        [Fact]
        public void Encoding_UTF8_FuncionaEmAmbos()
        {
            var path = Path.Combine(Path.GetTempPath(), $"encoding-test-{Guid.NewGuid()}.log");

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

                var logger = provider.CreateLogger("Test");

                // Caracteres UTF-8: português, emoji, chinês
                logger.LogInformation("Olá mundo 🌍 你好");

                provider.Dispose();

                var content = File.ReadAllText(path, Encoding.UTF8);

                Assert.Contains("Olá mundo 🌍 你好", content);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        // ✅ Teste 7: JSON com caracteres especiais
        [Fact]
        public void JSON_CaracteresEspeciais_FuncionaEmAmbos()
        {
            var path = Path.Combine(Path.GetTempPath(), $"json-test-{Guid.NewGuid()}.log");

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

                var logger = provider.CreateLogger("Test");

                logger.LogInformation("Teste com \"aspas\" e \\backslash\\ e \nnewline");

                provider.Dispose();

                var content = File.ReadAllText(path, Encoding.UTF8);
                var doc = System.Text.Json.JsonDocument.Parse(content.Trim());

                // JSON válido e preserva caracteres especiais
                Assert.NotNull(doc.RootElement);
                Assert.Contains("aspas", doc.RootElement.GetProperty("message").GetString());
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        // ────────────────────────────────────────
        // File I/O
        // ────────────────────────────────────────

        // ✅ Teste 8: FileStream assíncrono funciona em ambos
        [Fact]
        public async Task FileStream_Async_FuncionaEmAmbos()
        {
            var path = Path.Combine(Path.GetTempPath(), $"async-io-{Guid.NewGuid()}.log");

            try
            {
                var provider = new CustomLoggerProviderBuilder()
                    .WithOptions(opts =>
                    {
                        opts.MinimumLogLevel = LogLevel.Trace;
                        opts.UseGlobalBuffer = true;
                        opts.BatchOptions = new BatchOptions
                        {
                            BatchSize = 5,
                            FlushInterval = TimeSpan.Zero
                        };
                    })
                    .AddFileSink(path)
                    .Build();

                var logger = provider.CreateLogger("Test");

                await Task.Run(() =>
                {
                    for (int i = 0; i < 10; i++)
                    {
                        logger.LogInformation($"Async IO {i}");
                    }
                });

                provider.Dispose();

                var lines = File.ReadAllLines(path)
                    .Where(l => !string.IsNullOrWhiteSpace(l))
                    .ToArray();

                Assert.Equal(10, lines.Length);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        // ✅ Teste 9: Diretório profundo criado em ambos
        [Fact]
        public void Directory_CriacaoProfunda_FuncionaEmAmbos()
        {
            var deepPath = Path.Combine(
                Path.GetTempPath(),
                "deep", "nested", "structure", "test",
                $"file-{Guid.NewGuid()}.log"
            );

            try
            {
                var provider = new CustomLoggerProviderBuilder()
                    .WithOptions(opts =>
                    {
                        opts.MinimumLogLevel = LogLevel.Trace;
                        opts.UseGlobalBuffer = false;
                    })
                    .AddFileSink(deepPath)
                    .Build();

                var logger = provider.CreateLogger("Test");
                logger.LogInformation("Deep directory");

                provider.Dispose();

                Assert.True(File.Exists(deepPath));
            }
            finally
            {
                if (File.Exists(deepPath))
                    File.Delete(deepPath);

                // Cleanup diretórios
                var dir = Path.GetDirectoryName(deepPath);
                while (dir != Path.GetTempPath() && Directory.Exists(dir))
                {
                    try
                    {
                        Directory.Delete(dir, recursive: true);
                        break;
                    }
                    catch
                    {
                        dir = Path.GetDirectoryName(dir);
                    }
                }
            }
        }

        // ✅ Teste 10: File.Share permite leitura concorrente em ambos
        [Fact]
        public async Task FileShare_LeituraConcorrente_FuncionaEmAmbos()
        {
            var path = Path.Combine(Path.GetTempPath(), $"share-test-{Guid.NewGuid()}.log");

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

                var logger = provider.CreateLogger("Test");

                // Escrever em background
                var writeTask = Task.Run(() =>
                {
                    for (int i = 0; i < 50; i++)
                    {
                        logger.LogInformation($"Log {i}");
                        Thread.Sleep(5);
                    }
                });

                // Ler concorrentemente (FileShare.Read permite)
                await Task.Delay(50);
                var content = File.ReadAllText(path);

                await writeTask;
                provider.Dispose();

                // Leitura deve ter sucedido
                Assert.NotEmpty(content);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        // ────────────────────────────────────────
        // DateTimeOffset
        // ────────────────────────────────────────

        // ✅ Teste 11: DateTimeOffset.UtcNow funciona em ambos
        [Fact]
        public void DateTimeOffset_UtcNow_FuncionaEmAmbos()
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

            var logger = provider.CreateLogger("Test");
            var before = DateTimeOffset.UtcNow;

            logger.LogInformation("Timestamp test");

            var after = DateTimeOffset.UtcNow;
            provider.Dispose();

            var entry = mockSink.WrittenEntries.Single();

            Assert.True(entry.Timestamp >= before);
            Assert.True(entry.Timestamp <= after);
        }

        // ────────────────────────────────────────
        // System.Text.Json
        // ────────────────────────────────────────

        // ✅ Teste 12: System.Text.Json funciona em ambos
        [Fact]
        public void SystemTextJson_Serialization_FuncionaEmAmbos()
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

            var logger = provider.CreateLogger("Test");

            logger.LogInformation("JSON test");
            provider.Dispose();

            var entry = mockSink.WrittenEntries.Single();
            var formatter = new CustomLogger.Formatting.JsonLogFormatter();
            var json = formatter.Format(entry);

            // Deve parsear sem erro
            var doc = System.Text.Json.JsonDocument.Parse(json);
            Assert.Equal("JSON test", doc.RootElement.GetProperty("message").GetString());
        }
    }
}
