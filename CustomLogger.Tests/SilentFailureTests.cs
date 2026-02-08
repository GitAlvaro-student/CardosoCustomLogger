using CustomLogger.Abstractions;
using CustomLogger.Buffering;
using CustomLogger.Configurations;
using CustomLogger.Sinks;
using CustomLogger.Tests.Models;
using Microsoft.Extensions.Logging;
using System;

namespace CustomLogger.Tests
{
    public class SilentFailureTests
    {
        [Fact]
        public void All_Sinks_Failing_Should_Not_Throw()
        {
            // ARRANGE
            var composite = new CompositeLogSink(new ILogSink[]
            {
            new FailingSink(),
            new FailingSink(),
            new FailingSink()
            });

            var options = new CustomProviderOptions
            {
                UseGlobalBuffer = true,
                BatchOptions = new BatchOptions
                {
                    BatchSize = 1,
                    FlushIntervalMs = 0
                }
            };

            var buffer = new InstanceLogBuffer(composite, options);

            // ACT + ASSERT
            // O teste PASSA se nenhuma exceção for lançada
            buffer.Enqueue(new BufferedLogEntry(
                    DateTimeOffset.UtcNow,
                    "BackPressure",
                    LogLevel.Error,
                    new EventId(1, "DropOldest"),
                    $"Log 1",
                    new InvalidOperationException(),
                    null,
                    null
                ));
        }
    }
}
