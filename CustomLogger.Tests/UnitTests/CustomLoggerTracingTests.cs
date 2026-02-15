using CustomLogger.Configurations;
using CustomLogger.Tests.Mocks;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomLogger.Tests.UnitTests
{
    /// <summary>
    /// Testes para validar a captura automática de Activity.Current e configuração de contexto.
    /// </summary>
    public sealed class CustomLoggerTracingTests
    {
        // ────────────────────────────────────────
        // Activity.Current - TraceId e SpanId
        // ────────────────────────────────────────

        // ✅ Teste 1: Quando Activity.Current != null → TraceId e SpanId preenchidos
        [Fact]
        public void Log_ComActivityAtivo_TraceIdESpanIdPreenchidos()
        {
            var buffer = new MockLogBuffer();
            var logger = CriarLogger(buffer: buffer);

            using (var activity = new Activity("TestOperation").Start())
            {
                logger.LogInformation("Mensagem com activity");

                var entry = buffer.EnqueuedEntries.Single();
                Assert.NotNull(entry.TraceId);
                Assert.NotNull(entry.SpanId);
                Assert.Equal(activity.TraceId.ToString(), entry.TraceId);
                Assert.Equal(activity.SpanId.ToString(), entry.SpanId);
            }
        }

        // ✅ Teste 2: Quando Activity.Current == null → TraceId e SpanId são null
        [Fact]
        public void Log_SemActivity_TraceIdESpanIdNull()
        {
            var buffer = new MockLogBuffer();
            var logger = CriarLogger(buffer: buffer);

            // Garantir que não há Activity ativo
            Assert.Null(Activity.Current);

            logger.LogInformation("Mensagem sem activity");

            var entry = buffer.EnqueuedEntries.Single();
            Assert.Null(entry.TraceId);
            Assert.Null(entry.SpanId);
            Assert.Null(entry.ParentSpanId);
        }

        // ✅ Teste 3: Activity com ParentSpanId → ParentSpanId preenchido
        [Fact]
        public void Log_ComActivityAninhado_ParentSpanIdPreenchido()
        {
            var buffer = new MockLogBuffer();
            var logger = CriarLogger(buffer: buffer);

            using (var parentActivity = new Activity("ParentOperation").Start())
            {
                using (var childActivity = new Activity("ChildOperation").Start())
                {
                    logger.LogInformation("Mensagem em activity filho");

                    var entry = buffer.EnqueuedEntries.Single();
                    Assert.NotNull(entry.TraceId);
                    Assert.NotNull(entry.SpanId);
                    Assert.NotNull(entry.ParentSpanId);
                    Assert.Equal(childActivity.TraceId.ToString(), entry.TraceId);
                    Assert.Equal(childActivity.SpanId.ToString(), entry.SpanId);
                    Assert.Equal(childActivity.ParentSpanId.ToString(), entry.ParentSpanId);
                }
            }
        }

        // ✅ Teste 4: Múltiplos logs na mesma Activity → mesmo TraceId
        [Fact]
        public void Log_MultiplosLogsNaMesmaActivity_MesmoTraceId()
        {
            var buffer = new MockLogBuffer();
            var logger = CriarLogger(buffer: buffer);

            using (var activity = new Activity("TestOperation").Start())
            {
                logger.LogInformation("Log 1");
                logger.LogWarning("Log 2");
                logger.LogError("Log 3");

                Assert.Equal(3, buffer.EnqueuedEntries.Count);

                var traceId1 = buffer.EnqueuedEntries[0].TraceId;
                var traceId2 = buffer.EnqueuedEntries[1].TraceId;
                var traceId3 = buffer.EnqueuedEntries[2].TraceId;

                Assert.Equal(traceId1, traceId2);
                Assert.Equal(traceId2, traceId3);
                Assert.Equal(activity.TraceId.ToString(), traceId1);
            }
        }

        // ✅ Teste 5: Log após Activity parar → sem TraceId
        [Fact]
        public void Log_AposActivityParar_SemTraceId()
        {
            var buffer = new MockLogBuffer();
            var logger = CriarLogger(buffer: buffer);

            using (var activity = new Activity("TestOperation").Start())
            {
                // Log dentro da activity
                logger.LogInformation("Log 1");
            } // Activity parada aqui

            // Log fora da activity
            logger.LogInformation("Log 2");

            Assert.Equal(2, buffer.EnqueuedEntries.Count);
            Assert.NotNull(buffer.EnqueuedEntries[0].TraceId);
            Assert.Null(buffer.EnqueuedEntries[1].TraceId);
        }

        // ────────────────────────────────────────
        // ServiceName e Environment
        // ────────────────────────────────────────

        // ✅ Teste 6: ServiceName configurado → presente no log
        [Fact]
        public void Log_ComServiceNameConfigurado_ServiceNamePresente()
        {
            var buffer = new MockLogBuffer();
            var logger = CriarLogger(
                buffer: buffer,
                serviceName: "MeuServico"
            );

            logger.LogInformation("Teste");

            var entry = buffer.EnqueuedEntries.Single();
            Assert.Equal("MeuServico", entry.ServiceName);
        }

        // ✅ Teste 7: Environment configurado → presente no log
        [Fact]
        public void Log_ComEnvironmentConfigurado_EnvironmentPresente()
        {
            var buffer = new MockLogBuffer();
            var logger = CriarLogger(
                buffer: buffer,
                environment: "Production"
            );

            logger.LogInformation("Teste");

            var entry = buffer.EnqueuedEntries.Single();
            Assert.Equal("Production", entry.Environment);
        }

        // ✅ Teste 8: ServiceName e Environment não configurados → null
        [Fact]
        public void Log_SemContextoConfigurado_PropriedadesNull()
        {
            var buffer = new MockLogBuffer();
            var logger = CriarLogger(buffer: buffer);

            logger.LogInformation("Teste");

            var entry = buffer.EnqueuedEntries.Single();
            Assert.Null(entry.ServiceName);
            Assert.Null(entry.Environment);
        }

        // ✅ Teste 9: ServiceName e Environment configurados → ambos presentes
        [Fact]
        public void Log_ComContextoCompleto_AmbosPresentes()
        {
            var buffer = new MockLogBuffer();
            var logger = CriarLogger(
                buffer: buffer,
                serviceName: "MeuServico",
                environment: "Production"
            );

            logger.LogInformation("Teste");

            var entry = buffer.EnqueuedEntries.Single();
            Assert.Equal("MeuServico", entry.ServiceName);
            Assert.Equal("Production", entry.Environment);
        }

        // ✅ Teste 10: Múltiplos logs → mesmo ServiceName e Environment
        [Fact]
        public void Log_MultiplosLogs_MesmoContexto()
        {
            var buffer = new MockLogBuffer();
            var logger = CriarLogger(
                buffer: buffer,
                serviceName: "MeuServico",
                environment: "Staging"
            );

            logger.LogInformation("Log 1");
            logger.LogWarning("Log 2");
            logger.LogError("Log 3");

            Assert.Equal(3, buffer.EnqueuedEntries.Count);
            Assert.All(buffer.EnqueuedEntries, entry =>
            {
                Assert.Equal("MeuServico", entry.ServiceName);
                Assert.Equal("Staging", entry.Environment);
            });
        }

        // ────────────────────────────────────────
        // Combinação: Activity + Context
        // ────────────────────────────────────────

        // ✅ Teste 11: Activity + ServiceName + Environment → todos presentes
        [Fact]
        public void Log_ActivityComContexto_TodosPresentes()
        {
            var buffer = new MockLogBuffer();
            var logger = CriarLogger(
                buffer: buffer,
                serviceName: "MeuServico",
                environment: "Production"
            );

            using (var activity = new Activity("TestOperation").Start())
            {
                logger.LogInformation("Teste completo");

                var entry = buffer.EnqueuedEntries.Single();
                Assert.NotNull(entry.TraceId);
                Assert.NotNull(entry.SpanId);
                Assert.Equal("MeuServico", entry.ServiceName);
                Assert.Equal("Production", entry.Environment);
            }
        }

        // ────────────────────────────────────────
        // Helper
        // ────────────────────────────────────────
        private static Loggers.CustomLogger CriarLogger(
            MockLogBuffer buffer = null,
            string category = "TestCategory",
            LogLevel minimumLogLevel = LogLevel.Trace,
            string serviceName = null,
            string environment = null)
        {
            buffer ??= new MockLogBuffer();

            var options = new CustomProviderOptions
            {
                MinimumLogLevel = minimumLogLevel,
                ServiceName = serviceName,
                Environment = environment
            };

            var configuration = new CustomProviderConfiguration(options);
            var scopeProvider = new MockLogScopeProvider(null);

            return new Loggers.CustomLogger(category, configuration, buffer, scopeProvider);
        }
    }
}
