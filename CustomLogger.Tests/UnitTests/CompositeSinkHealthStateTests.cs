using CustomLogger.Abstractions;
using CustomLogger.Configurations;
using CustomLogger.Providers;
using CustomLogger.Sinks;
using CustomLogger.Tests.Mocks;
using CustomLogger.Tests.UnitTests;
using Microsoft.Extensions.Logging;

namespace CustomLogger.Tests.UnitTests
{
    public sealed class CompositeSinkHealthStateTests
    {
        private static CustomProviderOptions OptionsForSyncWrites()
        {
            return new CustomProviderOptions
            {
                MinimumLogLevel = LogLevel.Trace,
                UseGlobalBuffer = false
            };
        }

        [Fact]
        public void CompositeTracked_IncludesInnerSinks_HandlesNullInner_AndReportsDegraded()
        {
            // Arrange: inner sink will allow 1 write then fail -> Degradable wrapper will mark degraded after second write
            var failAfter = new FailAfterNSink(failAfter: 1);
            var degradableInner = new DegradableLogSink(failAfter);
            var composite = new CompositeLogSink(new ILogSink[] { degradableInner, null });

            var options = OptionsForSyncWrites();
            // Use the same composite as the provider sink and as the tracked sink
            var provider = new CustomLoggerProvider(options, composite, new ILogSink[] { composite });
            var logger = provider.CreateLogger("Test");

            // Act: first write succeeds, second write triggers failure and degradation
            logger.LogInformation("first");
            var before = ((ILoggingHealthState)provider).SinkStates;
            logger.LogInformation("second"); // triggers FailAfterNSink to throw inside degradable
            var after = ((ILoggingHealthState)provider).SinkStates;

            // Assert: before - inner was operational; after - inner degraded, null inner ignored
            Assert.NotNull(before);
            Assert.Single(before); // composite expands to its inner non-null sinks
            Assert.True(before[0].IsOperational);

            Assert.NotNull(after);
            Assert.Single(after);
            Assert.False(after[0].IsOperational);
            Assert.Equal("Degraded", after[0].StatusMessage);

            provider.Dispose();
        }

        //[Fact]
        //public void CompositeTracked_WithMultipleInners_ReportsMixedStates()
        //{
        //    // Arrange: first inner will degrade after second write; second inner remains healthy
        //    var failAfter = new FailAfterNSink(failAfter: 1);
        //    var degradableInner = new DegradableLogSink(failAfter);
        //    var healthyInner = new MockLogSink();

        //    var composite = new CompositeLogSink(new ILogSink[] { degradableInner, healthyInner });

        //    var provider = new CustomLoggerProviderBuilder()
        //        .WithOptions(opts =>
        //        {
        //            opts.MinimumLogLevel = LogLevel.Trace;
        //            opts.UseGlobalBuffer = false;
        //        })
        //        .AddSink(composite) // provider sink
        //        .BuildForTesting(composite); // see note below

        //    // NOTE: If your builder doesn't expose a Build overload accepting tracked sinks,
        //    // create provider directly to pass tracked sinks:
        //    // var provider = new CustomLoggerProvider(OptionsForSyncWrites(), composite, new ILogSink[] { composite });

        //    // For portability, we'll detect if builder returned a provider; fall back to direct construction.
        //    var p = provider ?? new CustomLoggerProvider(OptionsForSyncWrites(), composite, new ILogSink[] { composite });

        //    var logger = p.CreateLogger("Test");

        //    // Act - cause degradation on first tracked inner sink
        //    logger.LogInformation("first");  // success
        //    logger.LogInformation("second"); // triggers failure on failAfter

        //    var snapshots = ((ILoggingHealthState)p).SinkStates;

        //    // Assert - two inner sinks present, first degraded and second healthy
        //    Assert.Equal(2, snapshots.Count);

        //    Assert.False(snapshots[0].IsOperational);
        //    Assert.Equal("Degraded", snapshots[0].StatusMessage);

        //    Assert.True(snapshots[1].IsOperational);
        //    Assert.Null(snapshots[1].StatusMessage);

        //    p.Dispose();
        //}
    }
}