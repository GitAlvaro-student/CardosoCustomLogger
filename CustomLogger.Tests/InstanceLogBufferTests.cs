using CustomLogger.Buffering;
using CustomLogger.Configurations;
using CustomLogger.Sinks;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace CustomLogger.Tests
{
    public class InstanceLogBufferTests
    {
        [Fact]
        public void Should_Drop_Oldest_Logs_When_Buffer_Exceeds_Capacity()
        {
            // ARRANGE
            var options = new CustomProviderOptions
            {
                UseGlobalBuffer = true,
                BackpressureOptions = new BackpressureOptions
                {
                    MaxQueueCapacity = 5,
                    OverflowStrategy = OverflowStrategy.DropOldest
                },
                BatchOptions = new BatchOptions
                {
                    BatchSize = 100,          // Não vai fazer flush automático
                    FlushInterval = TimeSpan.Zero
                }
            };

            var mockSink = new MockLogSink();
            var buffer = new InstanceLogBuffer(mockSink, options);

            // ACT
            // Enviar 10 logs (capacidade = 5)
            for (int i = 0; i < 10; i++)
            {
                buffer.Enqueue(new BufferedLogEntry(
                    DateTimeOffset.UtcNow,
                    "BackPressure",
                    LogLevel.Error,
                    new EventId(i, "DropOldest"),
                    $"Log {i}",
                    new InvalidOperationException(),
                    null,
                    null
                ));
            }

            buffer.Flush();

            // ASSERT
            Assert.Equal(5, mockSink.WrittenEntries.Count);

            Assert.Equal("Log 5", mockSink.WrittenEntries[0].Message);
            Assert.Equal("Log 6", mockSink.WrittenEntries[1].Message);
            Assert.Equal("Log 7", mockSink.WrittenEntries[2].Message);
            Assert.Equal("Log 8", mockSink.WrittenEntries[3].Message);
            Assert.Equal("Log 9", mockSink.WrittenEntries[4].Message);

            Assert.Equal(5, buffer.GetDroppedLogsCount());
        }

        [Fact]
        public void Should_Drop_Newest_Logs_When_Buffer_Exceeds_Capacity()
        {
            // ARRANGE
            var options = new CustomProviderOptions
            {
                UseGlobalBuffer = true,
                BackpressureOptions = new BackpressureOptions
                {
                    MaxQueueCapacity = 5,
                    OverflowStrategy = OverflowStrategy.DropNewest
                },
                BatchOptions = new BatchOptions
                {
                    BatchSize = 100,
                    FlushInterval = TimeSpan.Zero
                }
            };

            var mockSink = new MockLogSink();
            var buffer = new InstanceLogBuffer(mockSink, options);

            // ACT
            for (int i = 0; i < 10; i++)
            {
                buffer.Enqueue(new BufferedLogEntry(
                    DateTimeOffset.UtcNow,
                    "BackPressure",
                    LogLevel.Error,
                    new EventId(i, "DropOldest"),
                    $"Log {i}",
                    new InvalidOperationException(),
                    null,
                    null
                ));
            }

            buffer.Flush();

            // ASSERT
            Assert.Equal(5, mockSink.WrittenEntries.Count);

            Assert.Equal("Log 0", mockSink.WrittenEntries[0].Message);
            Assert.Equal("Log 1", mockSink.WrittenEntries[1].Message);
            Assert.Equal("Log 2", mockSink.WrittenEntries[2].Message);
            Assert.Equal("Log 3", mockSink.WrittenEntries[3].Message);
            Assert.Equal("Log 4", mockSink.WrittenEntries[4].Message);

            Assert.Equal(5, buffer.GetDroppedLogsCount());
        }

        [Fact]
        public void Should_Block_When_Buffer_Is_Full_And_OverflowStrategy_Is_Block()
        {
            var options = new CustomProviderOptions
            {
                UseGlobalBuffer = true,
                BackpressureOptions = new BackpressureOptions
                {
                    MaxQueueCapacity = 5,
                    OverflowStrategy = OverflowStrategy.Block
                },
                BatchOptions = new BatchOptions { BatchSize = 100, FlushInterval = TimeSpan.Zero }
            };

            var mockSink = new MockLogSink();
            var buffer = new InstanceLogBuffer(mockSink, options);

            // Thread 1: Encher a fila
            var task1 = Task.Run(() =>
            {
                for (int i = 0; i < 5; i++)
                {
                    buffer.Enqueue(new BufferedLogEntry(
                        DateTimeOffset.UtcNow,
                        "BackPressure",
                        LogLevel.Error,
                        new EventId(i, "DropOldest"),
                        $"Log {i}",
                        new InvalidOperationException(),
                        null,
                        null
                    ));
                }
            });

            task1.Wait();

            // Thread 2: Tentar adicionar mais (deve bloquear)
            var task2 = Task.Run(() =>
            {
                var stopwatch = Stopwatch.StartNew();
                buffer.Enqueue(new BufferedLogEntry(
                    DateTimeOffset.UtcNow,
                    "BackPressure",
                    LogLevel.Error,
                    new EventId(5, "DropOldest"),
                    "Log 5",
                    new InvalidOperationException(),
                    null,
                    null
                )); return stopwatch.ElapsedMilliseconds;
            });

            // Thread 3: Fazer flush para liberar espaço
            Thread.Sleep(100);
            buffer.Flush();

            var elapsedMs = task2.Result;

            // ✅ ESPERADO:
            // - task2 bloqueou até flush liberar espaço
            // - elapsedMs > 50ms (bloqueou)
            // - Todos os 6 logs escritos
            // - GetDroppedLogsCount() == 0

            Assert.True(elapsedMs > 50);
            Assert.Equal(6, mockSink.WrittenEntries.Count);
            Assert.Equal(0, buffer.GetDroppedLogsCount());
        }

        [Fact]
        public async Task Should_Drop_Oldest_Logs_When_Buffer_Exceeds_Capacity_Async()
        {
            // ARRANGE
            var options = new CustomProviderOptions
            {
                UseGlobalBuffer = true,
                BackpressureOptions = new BackpressureOptions
                {
                    MaxQueueCapacity = 5,
                    OverflowStrategy = OverflowStrategy.DropOldest
                },
                BatchOptions = new BatchOptions
                {
                    BatchSize = 100,
                    FlushInterval = TimeSpan.Zero
                }
            };

            var mockSink = new MockLogSink();
            var buffer = new InstanceLogBuffer(mockSink, options);

            // ACT — Enviar 10 logs async
            for (int i = 0; i < 10; i++)
            {
                await buffer.EnqueueAsync(new BufferedLogEntry(
                    DateTimeOffset.UtcNow,
                    "BackPressure",
                    LogLevel.Error,
                    new EventId(i, "DropOldest"),
                    $"Log {i}",
                    new InvalidOperationException(),
                    null,
                    null
                ));
            }

            await buffer.FlushAsync();

            // ASSERT — Mesmo comportamento do Teste 1
            Assert.Equal(5, mockSink.WrittenEntries.Count);
            Assert.Equal("Log 5", mockSink.WrittenEntries[0].Message);
            Assert.Equal(5, buffer.GetDroppedLogsCount());
        }

        [Fact]
        public async Task Should_Drop_Newest_Logs_When_Buffer_Exceeds_Capacity_Async()
        {
            // ARRANGE
            var options = new CustomProviderOptions
            {
                UseGlobalBuffer = true,
                BackpressureOptions = new BackpressureOptions
                {
                    MaxQueueCapacity = 5,
                    OverflowStrategy = OverflowStrategy.DropNewest
                },
                BatchOptions = new BatchOptions
                {
                    BatchSize = 100,
                    FlushInterval = TimeSpan.Zero
                }
            };

            var mockSink = new MockLogSink();
            var buffer = new InstanceLogBuffer(mockSink, options);

            // ACT
            for (int i = 0; i < 10; i++)
            {
                await buffer.EnqueueAsync(new BufferedLogEntry(
                    DateTimeOffset.UtcNow,
                    "BackPressure",
                    LogLevel.Error,
                    new EventId(i, "DropOldest"),
                    $"Log {i}",
                    new InvalidOperationException(),
                    null,
                    null
                ));
            }

            buffer.Flush();

            // ASSERT
            Assert.Equal(5, mockSink.WrittenEntries.Count);

            Assert.Equal("Log 0", mockSink.WrittenEntries[0].Message);
            Assert.Equal("Log 1", mockSink.WrittenEntries[1].Message);
            Assert.Equal("Log 2", mockSink.WrittenEntries[2].Message);
            Assert.Equal("Log 3", mockSink.WrittenEntries[3].Message);
            Assert.Equal("Log 4", mockSink.WrittenEntries[4].Message);

            Assert.Equal(5, buffer.GetDroppedLogsCount());
        }

        [Fact]
        public void Should_Handle_Backpressure_Under_High_Concurrent_Load()
        {
            // ARRANGE
            var options = new CustomProviderOptions
            {
                UseGlobalBuffer = true,
                BackpressureOptions = new BackpressureOptions
                {
                    MaxQueueCapacity = 1000,
                    OverflowStrategy = OverflowStrategy.DropOldest
                },
                BatchOptions = new BatchOptions
                {
                    BatchSize = 50,
                    FlushInterval = TimeSpan.FromSeconds(1)
                }
            };

            var mockSink = new MockLogSink();
            var buffer = new InstanceLogBuffer(mockSink, options);

            // ACT — 10.000 logs em paralelo
            var tasks = Enumerable.Range(0, 10_000)
                .Select(i => Task.Run(() =>
                    buffer.Enqueue(new BufferedLogEntry(
                    DateTimeOffset.UtcNow,
                    "BackPressure",
                    LogLevel.Error,
                    new EventId(i, "DropOldest"),
                    $"Log {i}",
                    new InvalidOperationException(),
                    null,
                    null
                ))

                ))
                .ToArray();

            Task.WaitAll(tasks);

            buffer.Flush();

            // ASSERT
            Assert.True(buffer.GetDroppedLogsCount() > 0);
            Assert.True(mockSink.WrittenEntries.Count <= 1000);
        }

    }
}