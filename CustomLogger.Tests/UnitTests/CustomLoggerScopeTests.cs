using CustomLogger.Configurations;
using CustomLogger.Scopes;
using CustomLogger.Tests.Mocks;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CustomLogger.Tests.PureUnits
{
    public sealed class CustomLoggerScopeTests
    {
        // ────────────────────────────────────────
        // BeginScope
        // ────────────────────────────────────────

        // ✅ Teste 1: BeginScope simples → aplicado ao log
        [Fact]
        public void BeginScope_ScopeUnico_PreservadoNoLog()
        {
            var buffer = new MockLogBuffer();
            var logger = CriarLogger(buffer);

            using (logger.BeginScope(new Dictionary<string, object>
            {
                ["RequestId"] = "REQ-1"
            }))
            {
                logger.LogInformation("Test");
            }

            var entry = buffer.EnqueuedEntries.Single();
            Assert.Equal("REQ-1", entry.Scopes["RequestId"]);
        }

        // ✅ Teste 2: Scopes aninhados → combinados corretamente
        [Fact]
        public void BeginScope_ScopesAninhados_Combinados()
        {
            var buffer = new MockLogBuffer();
            var logger = CriarLogger(buffer);

            using (logger.BeginScope(new Dictionary<string, object>
            {
                ["UserId"] = 10
            }))
            using (logger.BeginScope(new Dictionary<string, object>
            {
                ["Operation"] = "CreateOrder"
            }))
            {
                logger.LogInformation("Test");
            }

            var entry = buffer.EnqueuedEntries.Single();

            Assert.Equal(10, entry.Scopes["UserId"]);
            Assert.Equal("CreateOrder", entry.Scopes["Operation"]);
        }

        // ✅ Teste 3: Dispose do scope → contexto removido
        [Fact]
        public void BeginScope_Dispose_RemoveScope()
        {
            var buffer = new MockLogBuffer();
            var logger = CriarLogger(buffer);

            using (logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = "C-123"
            }))
            {
                logger.LogInformation("Inside");
            }

            logger.LogInformation("Outside");

            Assert.Equal(2, buffer.EnqueuedEntries.Count);

            Assert.Equal("C-123", buffer.EnqueuedEntries[0].Scopes["CorrelationId"]);
            Assert.Empty(buffer.EnqueuedEntries[1].Scopes);
        }

        // ────────────────────────────────────────
        // Activity / Correlação
        // ────────────────────────────────────────

        // ✅ Teste 4: Scope + Activity → TraceId e SpanId presentes
        [Fact]
        public void Log_ComScopeEActivity_PreservaTraceESpan()
        {
            var buffer = new MockLogBuffer();
            var logger = CriarLogger(buffer);

            using var activity = new Activity("test-activity");
            activity.Start();

            using (logger.BeginScope(new Dictionary<string, object>
            {
                ["Operation"] = "Payment",
                ["traceId"] = activity.Context.TraceId.ToString(),
                ["spanId"] = activity.Context.SpanId.ToString()
            }))
            {
                logger.LogInformation("Test");
            }

            activity.Stop();

            var entry = buffer.EnqueuedEntries.Single();

            Assert.Equal("Payment", entry.Scopes["Operation"]);

            Assert.True(entry.Scopes.ContainsKey("traceId"));
            Assert.True(entry.Scopes.ContainsKey("spanId"));
        }


        // ────────────────────────────────────────
        // Helper
        // ────────────────────────────────────────

        private static Loggers.CustomLogger CriarLogger(
    MockLogBuffer buffer,
    string category = "TestCategory")
        {
            var options = new CustomProviderOptions
            {
                MinimumLogLevel = LogLevel.Trace
            };

            var configuration = new CustomProviderConfiguration(options);
            var scopeProvider = new LogScopeProvider(); // ✅ padrão do projeto

            return new Loggers.CustomLogger(
                category,
                configuration,
                buffer,
                scopeProvider
            );
        }

    }
}
