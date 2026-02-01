using CustomLogger.Abstractions;
using CustomLogger.Buffering;
using CustomLogger.Configurations;
using CustomLogger.Sinks;
using CustomLogger.Tests.Models;
using Microsoft.Extensions.Logging;

namespace CustomLogger.Tests
{
    public class AsyncFallbackTests
    {
        [Fact]
        public async Task FlushAsync_With_Failing_Sink_Should_Fallback()
        {
            // ARRANGE
            var failingSink = new FailingSink();
            var mockSink = new Models.MockLogSink();

            var composite = new CompositeLogSink(
                new ILogSink[] { failingSink, mockSink }
            );

            var options = new CustomProviderOptions
            {
                UseGlobalBuffer = true,
                BatchOptions = new BatchOptions
                {
                    BatchSize = 1,
                    FlushInterval = TimeSpan.Zero
                }
            };

            var buffer = new InstanceLogBuffer(composite, options);

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


            await buffer.FlushAsync();

            // ASSERT
            Assert.Equal(1, mockSink.WrittenEntries.Count);
        }
    }
}
