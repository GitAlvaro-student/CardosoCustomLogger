using CustomLogger.Abstractions;
using CustomLogger.Buffering;
using CustomLogger.Configurations;
using CustomLogger.Sinks;
using CustomLogger.Tests.Mocks;
using CustomLogger.Tests.Models;
using Microsoft.Extensions.Logging;
using System;
using Xunit;

namespace CustomLogger.Tests
{
    public class FallbackTests
    {
        [Fact]
        public void Sink_Failure_Should_Not_Break_Logging_Pipeline()
        {
            // ARRANGE
            var failingSink = new FailingSink();
            var mockSink = new MockLogSink();

            var compositeSink = new CompositeLogSink(
                new ILogSink[] { failingSink, mockSink }
            );

            var options = new CustomProviderOptions
            {
                UseGlobalBuffer = true,
                BatchOptions = new BatchOptions
                {
                    BatchSize = 1,
                    FlushIntervalMs = 0
                }
            };

            var buffer = new InstanceLogBuffer(compositeSink, options);

            // ACT
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

            buffer.Flush();

            // ASSERT
            Assert.Equal(1, mockSink.WrittenEntries.Count);
        }
    }
}
