using CustomLogger.Abstractions;
using CustomLogger.HealthChecks;
using CustomLogger.HealthChecks.Models;
using CustomLogger.Sinks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomLogger.Tests.HealthChecks
{
    public class DefaultLoggingHealthEvaluatorTests
    {
        private readonly DefaultLoggingHealthEvaluator _evaluator;

        public DefaultLoggingHealthEvaluatorTests()
        {
            _evaluator = new DefaultLoggingHealthEvaluator();
        }

        #region Buffer Tests

        [Fact]
        public void Buffer_At50Percent_ReturnsHealthy()
        {
            // Arrange
            var sink = new SinkHealthSnapshot(
                name: "HealthySink",
                type: "FileSink",
                isOperational: true,
                statusMessage: null,
                lastSuccessfulWriteUtc: DateTime.UtcNow
            );

            var state = new FakeLoggingHealthState
            {
                MaxBufferCapacity = 1000,
                CurrentBufferSize = 500,
                IsDiscardingMessages = false,
                IsBlocking = false,
                IsDegradedMode = false,
                SinkStates = new List<SinkHealthSnapshot> { sink }
            };

            // Act
            var report = _evaluator.Evaluate(state);

            // Assert
            Assert.Equal(LoggingHealthStatus.Healthy, report.Status);
            Assert.Equal(50.0, report.BufferUsagePercentage);
        }


        [Fact]
        public void Buffer_At85Percent_ReturnsDegraded()
        {
            // Arrange
            var state = new FakeLoggingHealthState
            {
                MaxBufferCapacity = 1000,
                CurrentBufferSize = 850,
                IsDiscardingMessages = false,
                IsBlocking = false,
                IsDegradedMode = false,
                SinkStates = new List<SinkHealthSnapshot>()
            };

            // Act
            var report = _evaluator.Evaluate(state);

            // Assert
            Assert.Equal(LoggingHealthStatus.Degraded, report.Status);
            Assert.Equal(85.0, report.BufferUsagePercentage);
            Assert.Contains(report.Issues, i => i.Component == "Buffer" && i.Severity == LoggingHealthStatus.Degraded);
        }

        [Fact]
        public void Buffer_At100Percent_ReturnsUnhealthy()
        {
            // Arrange
            var state = new FakeLoggingHealthState
            {
                MaxBufferCapacity = 1000,
                CurrentBufferSize = 1000,
                IsDiscardingMessages = false,
                IsBlocking = false,
                IsDegradedMode = false,
                SinkStates = new List<SinkHealthSnapshot>()
            };

            // Act
            var report = _evaluator.Evaluate(state);

            // Assert
            Assert.Equal(LoggingHealthStatus.Unhealthy, report.Status);
            Assert.Equal(100.0, report.BufferUsagePercentage);
            Assert.Contains(report.Issues, i => i.Component == "Buffer" && i.Severity == LoggingHealthStatus.Unhealthy);
        }

        [Fact]
        public void Buffer_IsDiscarding_ReturnsUnhealthy()
        {
            // Arrange
            var state = new FakeLoggingHealthState
            {
                MaxBufferCapacity = 1000,
                CurrentBufferSize = 800,
                IsDiscardingMessages = true,
                IsBlocking = false,
                IsDegradedMode = false,
                SinkStates = new List<SinkHealthSnapshot>()
            };

            // Act
            var report = _evaluator.Evaluate(state);

            // Assert
            Assert.Equal(LoggingHealthStatus.Unhealthy, report.Status);
            Assert.Contains(report.Issues, i => i.Component == "Buffer" && i.Description.Contains("discarding"));
        }

        #endregion

        #region Sink Tests

        [Fact]
        public void Sink_IsolatedFailureWithFallback_ReturnsDegraded()
        {
            // Arrange
            var sink = new SinkHealthSnapshot(
                name: "PrimarySink",
                type: "FileSink",
                isOperational: true,
                statusMessage: "Using fallback path",
                lastSuccessfulWriteUtc: DateTime.UtcNow.AddSeconds(-10)
            );

            var state = new FakeLoggingHealthState
            {
                MaxBufferCapacity = 1000,
                CurrentBufferSize = 500,
                IsDiscardingMessages = false,
                IsBlocking = false,
                IsDegradedMode = false,
                SinkStates = new List<SinkHealthSnapshot> { sink }
            };

            // Act
            var report = _evaluator.Evaluate(state);

            // Assert
            Assert.Equal(LoggingHealthStatus.Degraded, report.Status);
            Assert.Contains(report.Issues, i => i.Component.Contains("PrimarySink") && i.Severity == LoggingHealthStatus.Degraded);
        }

        [Fact]
        public void Sink_ContinuousFailure_ReturnsUnhealthy()
        {
            // Arrange
            var sink = new SinkHealthSnapshot(
                name: "FailingSink",
                type: "DatabaseSink",
                isOperational: true,
                statusMessage: "Operational",
                lastSuccessfulWriteUtc: DateTime.UtcNow.AddMinutes(-6)
            );

            var state = new FakeLoggingHealthState
            {
                MaxBufferCapacity = 1000,
                CurrentBufferSize = 500,
                IsDiscardingMessages = false,
                IsBlocking = false,
                IsDegradedMode = false,
                SinkStates = new List<SinkHealthSnapshot> { sink }
            };

            // Act
            var report = _evaluator.Evaluate(state);

            // Assert
            Assert.Equal(LoggingHealthStatus.Unhealthy, report.Status);
            Assert.Contains(report.Issues, i => i.Component.Contains("FailingSink") && i.Severity == LoggingHealthStatus.Unhealthy);
        }

        [Fact]
        public void Sink_NotOperational_ReturnsUnhealthy()
        {
            // Arrange
            var sink = new SinkHealthSnapshot(
                name: "DisabledSink",
                type: "ConsoleSink",
                isOperational: false,
                statusMessage: "Sink disabled due to repeated failures",
                lastSuccessfulWriteUtc: DateTime.UtcNow.AddMinutes(-10)
            );

            var state = new FakeLoggingHealthState
            {
                MaxBufferCapacity = 1000,
                CurrentBufferSize = 500,
                IsDiscardingMessages = false,
                IsBlocking = false,
                IsDegradedMode = false,
                SinkStates = new List<SinkHealthSnapshot> { sink }
            };

            // Act
            var report = _evaluator.Evaluate(state);

            // Assert
            Assert.Equal(LoggingHealthStatus.Unhealthy, report.Status);
            Assert.Contains(report.Issues, i => i.Component.Contains("DisabledSink") && i.Severity == LoggingHealthStatus.Unhealthy);
        }

        [Fact]
        public void Sink_InDegradedMode_ReturnsDegraded()
        {
            // Arrange
            var sink = new SinkHealthSnapshot(
                name: "DegradedSink",
                type: "NetworkSink",
                isOperational: true,
                statusMessage: "Operating in degraded mode",
                lastSuccessfulWriteUtc: DateTime.UtcNow.AddSeconds(-5)
            );

            var state = new FakeLoggingHealthState
            {
                MaxBufferCapacity = 1000,
                CurrentBufferSize = 500,
                IsDiscardingMessages = false,
                IsBlocking = false,
                IsDegradedMode = false,
                SinkStates = new List<SinkHealthSnapshot> { sink }
            };

            // Act
            var report = _evaluator.Evaluate(state);

            // Assert
            Assert.Equal(LoggingHealthStatus.Degraded, report.Status);
            Assert.Contains(report.Issues, i => i.Component.Contains("DegradedSink") && i.Severity == LoggingHealthStatus.Degraded);
        }

        #endregion

        #region Degraded Mode Tests

        [Fact]
        public void DegradedMode_Active_NeverReturnsHealthy()
        {
            // Arrange
            var sink = new SinkHealthSnapshot(
                name: "HealthySink",
                type: "FileSink",
                isOperational: true,
                statusMessage: "Operating normally",
                lastSuccessfulWriteUtc: DateTime.UtcNow.AddSeconds(-1)
            );

            var state = new FakeLoggingHealthState
            {
                MaxBufferCapacity = 1000,
                CurrentBufferSize = 100,
                IsDiscardingMessages = false,
                IsBlocking = false,
                IsDegradedMode = true,
                SinkStates = new List<SinkHealthSnapshot> { sink }
            };

            // Act
            var report = _evaluator.Evaluate(state);

            // Assert
            Assert.NotEqual(LoggingHealthStatus.Healthy, report.Status);
            Assert.Equal(LoggingHealthStatus.Degraded, report.Status);
            Assert.Contains(report.Issues, i => i.Component == "Provider" && i.Description.Contains("degraded mode"));
        }

        [Fact]
        public void DegradedMode_Active_MinimumDegraded()
        {
            // Arrange
            var state = new FakeLoggingHealthState
            {
                MaxBufferCapacity = 1000,
                CurrentBufferSize = 50,
                IsDiscardingMessages = false,
                IsBlocking = false,
                IsDegradedMode = true,
                SinkStates = new List<SinkHealthSnapshot>()
            };

            // Act
            var report = _evaluator.Evaluate(state);

            // Assert
            Assert.True(report.Status >= LoggingHealthStatus.Degraded);
            Assert.Contains(report.Issues, i => i.Severity == LoggingHealthStatus.Degraded);
        }

        #endregion

        #region Aggregation Tests

        [Fact]
        public void Aggregation_BufferDegradedAndSinkHealthy_ReturnsDegraded()
        {
            // Arrange
            var sink = new SinkHealthSnapshot(
                name: "HealthySink",
                type: "FileSink",
                isOperational: true,
                statusMessage: null,
                lastSuccessfulWriteUtc: DateTime.UtcNow
            );

            var state = new FakeLoggingHealthState
            {
                MaxBufferCapacity = 1000,
                CurrentBufferSize = 850,
                IsDiscardingMessages = false,
                IsBlocking = false,
                IsDegradedMode = false,
                SinkStates = new List<SinkHealthSnapshot> { sink }
            };

            // Act
            var report = _evaluator.Evaluate(state);

            // Assert
            Assert.Equal(LoggingHealthStatus.Degraded, report.Status);
        }

        [Fact]
        public void Aggregation_BufferHealthyAndSinkUnhealthy_ReturnsUnhealthy()
        {
            // Arrange
            var sink = new SinkHealthSnapshot(
                name: "FailedSink",
                type: "DatabaseSink",
                isOperational: false,
                statusMessage: "Connection failed",
                lastSuccessfulWriteUtc: null
            );

            var state = new FakeLoggingHealthState
            {
                MaxBufferCapacity = 1000,
                CurrentBufferSize = 500,
                IsDiscardingMessages = false,
                IsBlocking = false,
                IsDegradedMode = false,
                SinkStates = new List<SinkHealthSnapshot> { sink }
            };

            // Act
            var report = _evaluator.Evaluate(state);

            // Assert
            Assert.Equal(LoggingHealthStatus.Unhealthy, report.Status);
        }

        [Fact]
        public void Aggregation_MultipleIssues_WorstCaseWins()
        {
            // Arrange
            var sink1 = new SinkHealthSnapshot(
                name: "Sink1",
                type: "FileSink",
                isOperational: true,
                statusMessage: "fallback active",
                lastSuccessfulWriteUtc: DateTime.UtcNow
            );

            var sink2 = new SinkHealthSnapshot(
                name: "Sink2",
                type: "DatabaseSink",
                isOperational: false,
                statusMessage: "Failed",
                lastSuccessfulWriteUtc: null
            );

            var state = new FakeLoggingHealthState
            {
                MaxBufferCapacity = 1000,
                CurrentBufferSize = 850,
                IsDiscardingMessages = false,
                IsBlocking = false,
                IsDegradedMode = false,
                SinkStates = new List<SinkHealthSnapshot> { sink1, sink2 }
            };

            // Act
            var report = _evaluator.Evaluate(state);

            // Assert
            Assert.Equal(LoggingHealthStatus.Unhealthy, report.Status);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void Evaluate_NullState_ReturnsUnknown()
        {
            // Act
            var report = _evaluator.Evaluate(null);

            // Assert
            Assert.Equal(LoggingHealthStatus.Unknown, report.Status);
            Assert.NotNull(report);
        }

        [Fact]
        public void Evaluate_NoSinks_ReturnsDegraded()
        {
            // Arrange
            var state = new FakeLoggingHealthState
            {
                MaxBufferCapacity = 1000,
                CurrentBufferSize = 500,
                IsDiscardingMessages = false,
                IsBlocking = false,
                IsDegradedMode = false,
                SinkStates = new List<SinkHealthSnapshot>()
            };

            // Act
            var report = _evaluator.Evaluate(state);

            // Assert
            Assert.Equal(LoggingHealthStatus.Degraded, report.Status);
            Assert.Contains(report.Issues, i => i.Component == "Sinks" && i.Description.Contains("No sinks configured"));
        }

        #endregion
    }

    #region Fake Implementation

    internal class FakeLoggingHealthState : ILoggingHealthState
    {
        public int MaxBufferCapacity { get; set; }
        public int CurrentBufferSize { get; set; }
        public bool IsDiscardingMessages { get; set; }
        public bool IsBlocking { get; set; }
        public bool IsDegradedMode { get; set; }
        public IReadOnlyList<SinkHealthSnapshot> SinkStates { get; set; }
    }

    #endregion

}
