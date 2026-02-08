using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomLogger.Tests
{
    using CustomLogger.Buffering;
    using CustomLogger.Configurations;
    using CustomLogger.Tests.Models;
    using Microsoft.Extensions.Logging;
    using Xunit;

    public class BatchFallbackTests
    {
        [Fact]
        public void WriteBatch_Failure_Should_Fallback_To_Individual_Write()
        {
            // ARRANGE
            var sink = new FailingBatchSink();

            var options = new CustomProviderOptions
            {
                UseGlobalBuffer = true,
                BatchOptions = new BatchOptions
                {
                    BatchSize = 10,
                    FlushIntervalMs = 0
                }
            };

            var buffer = new InstanceLogBuffer(sink, options);

            // ACT
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

            buffer.Flush();

            // ASSERT
            Assert.Equal(5, sink.WrittenEntries.Count);
        }
    }

}
