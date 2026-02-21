using CustomLogger.Abstractions;
using CustomLogger.Configurations;
using CustomLogger.Providers;
using CustomLogger.Sinks;
using CustomLogger.Tests.Mocks;
using CustomLogger.Tests.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomLogger.Tests.UnitTests
{
    public sealed class LoggingHealthStateTests
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
        public void SinkStates_NoTrackedSinks_ReturnsEmpty()
        {
            // Arrange
            var options = OptionsForSyncWrites();
            var composite = new CompositeLogSink(new ILogSink[0]); // no sinks
            var provider = new CustomLoggerProvider(options, composite, sinksToTrack: null);

            var state = (ILoggingHealthState)provider;

            // Act
            var snapshots = state.SinkStates;

            // Assert
            Assert.NotNull(snapshots);
            Assert.Empty(snapshots);

            provider.Dispose();
        }

        [Fact]
        public void SinkStates_AllSinksHealthy_ReturnsOperationalTrue()
        {
            // Arrange
            var options = OptionsForSyncWrites();

            var provider = new CustomLoggerProviderBuilder()
                .WithOptions(opts =>
                {
                    opts.MinimumLogLevel = LogLevel.Trace;
                    opts.UseGlobalBuffer = false;
                })
                .AddSinkWithoutDegradation(new MockLogSink())
                .Build();

            var state = (ILoggingHealthState)provider;

            // Act
            var snapshots = state.SinkStates;

            // Assert
            Assert.Single(snapshots);
            Assert.True(snapshots[0].IsOperational);
            Assert.Null(snapshots[0].StatusMessage);

            provider.Dispose();
        }

        [Fact]
        public void SinkStates_SinkAlwaysFails_IsMarkedDegraded()
        {
            // Arrange
            var provider = new CustomLoggerProviderBuilder()
                .WithOptions(opts =>
                {
                    opts.MinimumLogLevel = LogLevel.Trace;
                    opts.UseGlobalBuffer = false;
                })
                .AddSinkWithDegradation(new FailingSink()) // wrapped in DegradableLogSink
                .Build();

            var logger = provider.CreateLogger("Test");

            // Act - first write fails and should mark degradado
            logger.LogInformation("trigger failure");

            var snapshots = ((ILoggingHealthState)provider).SinkStates;

            // Assert
            Assert.Single(snapshots);
            Assert.False(snapshots[0].IsOperational);
            Assert.Equal("Degraded", snapshots[0].StatusMessage);

            provider.Dispose();
        }

        [Fact]
        public void SinkStates_FailAfterN_FailsAfterThresholdAndShowsDegraded()
        {
            // Arrange: sink allows 1 write then fails
            var failAfter = new FailAfterNSink(failAfter: 1);
            var provider = new CustomLoggerProviderBuilder()
                .WithOptions(opts =>
                {
                    opts.MinimumLogLevel = LogLevel.Trace;
                    opts.UseGlobalBuffer = false;
                })
                .AddSinkWithDegradation(failAfter) // degradable wrapper
                .Build();

            var logger = provider.CreateLogger("Test");

            // Act - first write succeeds (not degraded), second write triggers degradation
            logger.LogInformation("ok #1");
            var before = ((ILoggingHealthState)provider).SinkStates;
            logger.LogInformation("ok #2 -> should fail");
            var after = ((ILoggingHealthState)provider).SinkStates;

            // Assert
            Assert.Single(before);
            Assert.True(before[0].IsOperational);

            Assert.Single(after);
            Assert.False(after[0].IsOperational);
            Assert.Equal("Degraded", after[0].StatusMessage);

            provider.Dispose();
        }

        [Fact]
        public void SinkStates_MultipleSinks_MixedStatesReported()
        {
            // Arrange: first sink will degrade after second write, second sink always healthy
            var failAfter = new FailAfterNSink(failAfter: 1);
            var healthy = new MockLogSink();

            var provider = new CustomLoggerProviderBuilder()
                .WithOptions(opts =>
                {
                    opts.MinimumLogLevel = LogLevel.Trace;
                    opts.UseGlobalBuffer = false;
                })
                .AddSinkWithDegradation(failAfter) // tracked as degradable
                .AddSinkWithoutDegradation(healthy)
                .Build();

            var logger = provider.CreateLogger("Test");

            // Act - cause degradation on first tracked sink
            logger.LogInformation("first");  // success
            logger.LogInformation("second"); // triggers failure on failAfter

            var snapshots = ((ILoggingHealthState)provider).SinkStates;

            // Assert - two sinks present, one degraded and one healthy
            Assert.Equal(2, snapshots.Count);

            // The first added sink is the degradable one
            Assert.False(snapshots[0].IsOperational);
            Assert.Equal("Degraded", snapshots[0].StatusMessage);

            Assert.True(snapshots[1].IsOperational);
            Assert.Null(snapshots[1].StatusMessage);

            provider.Dispose();
        }

        [Fact]
        public void SinkStates_TransitionHealthyToDegraded_HonorsStateChange()
        {
            // Arrange: failAfter = 1 -> healthy on first write, degrade on second
            var failAfter = new FailAfterNSink(failAfter: 1);

            var provider = new CustomLoggerProviderBuilder()
                .WithOptions(opts =>
                {
                    opts.MinimumLogLevel = LogLevel.Trace;
                    opts.UseGlobalBuffer = false;
                })
                .AddSinkWithDegradation(failAfter)
                .Build();

            var logger = provider.CreateLogger("Test");

            // Act & Assert - before second write: healthy
            logger.LogInformation("one");
            var s1 = ((ILoggingHealthState)provider).SinkStates;
            Assert.Single(s1);
            Assert.True(s1[0].IsOperational);

            // Act - second write triggers degradation
            logger.LogInformation("two");
            var s2 = ((ILoggingHealthState)provider).SinkStates;
            Assert.Single(s2);
            Assert.False(s2[0].IsOperational);
            Assert.Equal("Degraded", s2[0].StatusMessage);

            provider.Dispose();
        }
    }
}
