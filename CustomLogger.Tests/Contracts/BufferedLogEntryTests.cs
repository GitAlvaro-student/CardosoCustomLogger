using CustomLogger.Abstractions;
using CustomLogger.Buffering;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomLogger.Tests.Contracts
{
    public sealed class BufferedLogEntryTests
    {
        // ✅ Teste 1: Implementa ILogEntry corretamente
        [Fact]
        public void BufferedLogEntry_Implementa_ILogEntry()
        {
            ILogEntry entry = CriarEntry();

            Assert.IsAssignableFrom<ILogEntry>(entry);
        }

        // ✅ Teste 2: Campos obrigatórios preservados
        [Fact]
        public void BufferedLogEntry_Preserva_CamposObrigatorios()
        {
            var timestamp = DateTimeOffset.UtcNow;
            var eventId = new EventId(1, "TestEvent");

            var entry = CriarEntry(
                timestamp: timestamp,
                category: "TestCategory",
                logLevel: LogLevel.Warning,
                eventId: eventId,
                message: "Mensagem de teste"
            );

            Assert.Equal(timestamp, entry.Timestamp);
            Assert.Equal("TestCategory", entry.Category);
            Assert.Equal(LogLevel.Warning, entry.LogLevel);
            Assert.Equal(eventId, entry.EventId);
            Assert.Equal("Mensagem de teste", entry.Message);
        }

        // ✅ Teste 3: Sem exception
        [Fact]
        public void BufferedLogEntry_SemException_ExceptionNula()
        {
            var entry = CriarEntry(exception: null);

            Assert.Null(entry.Exception);
        }

        // ✅ Teste 4: Com exception preservada
        [Fact]
        public void BufferedLogEntry_ComException_PreservaException()
        {
            var exception = new InvalidOperationException("Erro de teste");
            var entry = CriarEntry(exception: exception);

            Assert.Equal(exception, entry.Exception);
            Assert.Equal("Erro de teste", entry.Exception.Message);
        }

        // ✅ Teste 5: Com exception aninhada
        [Fact]
        public void BufferedLogEntry_ComExceptionAninhada_PreservaInnerException()
        {
            var inner = new ArgumentException("Erro interno");
            var outer = new InvalidOperationException("Erro externo", inner);
            var entry = CriarEntry(exception: outer);

            Assert.Equal(outer, entry.Exception);
            Assert.Equal(inner, entry.Exception.InnerException);
        }

        // ✅ Teste 6: Scopes vazios
        [Fact]
        public void BufferedLogEntry_ScopesVazios_RetornaCollectionVazia()
        {
            var entry = CriarEntry(scopes: new Dictionary<string, object>());

            Assert.NotNull(entry.Scopes);
            Assert.Empty(entry.Scopes);
        }

        // ✅ Teste 7: Com múltiplos scopes
        [Fact]
        public void BufferedLogEntry_ComMultiplosScopes_PreservaScopes()
        {
            var scopes = new Dictionary<string, object>
            {
                ["RequestId"] = "abc-123",
                ["UserId"] = 42,
                ["Feature"] = "checkout"
            };

            var entry = CriarEntry(scopes: scopes);

            Assert.Equal(3, entry.Scopes.Count);
            Assert.Equal("abc-123", entry.Scopes["RequestId"]);
            Assert.Equal(42, entry.Scopes["UserId"]);
            Assert.Equal("checkout", entry.Scopes["Feature"]);
        }

        // ✅ Teste 8: State preservado
        [Fact]
        public void BufferedLogEntry_ComState_PreservaState()
        {
            var state = new { OrderId = 123, Status = "pending" };
            var entry = CriarEntry(state: state);

            Assert.Equal(state, entry.State);
        }

        // ✅ Teste 9: State nulo
        [Fact]
        public void BufferedLogEntry_SemState_StateNulo()
        {
            var entry = CriarEntry(state: null);

            Assert.Null(entry.State);
        }

        // ✅ Teste 10: Scopes é IReadOnlyDictionary (imutável)
        [Fact]
        public void BufferedLogEntry_Scopes_EhImmutable()
        {
            var scopes = new Dictionary<string, object> { ["Key"] = "Value" };
            var entry = CriarEntry(scopes: scopes);

            Assert.IsAssignableFrom<IReadOnlyDictionary<string, object>>(entry.Scopes);
        }

        // ✅ Teste 11: Propriedades de tracing quando não fornecidas → null
        [Fact]
        public void BufferedLogEntry_SemTracing_PropriedadesNull()
        {
            var entry = CriarEntry();

            Assert.Null(entry.TraceId);
            Assert.Null(entry.SpanId);
            Assert.Null(entry.ParentSpanId);
        }

        // ✅ Teste 12: TraceId fornecido → preservado
        [Fact]
        public void BufferedLogEntry_ComTraceId_PreservaTraceId()
        {
            var traceId = "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01";
            var entry = CriarEntry(traceId: traceId);

            Assert.Equal(traceId, entry.TraceId);
        }

        // ✅ Teste 13: SpanId fornecido → preservado
        [Fact]
        public void BufferedLogEntry_ComSpanId_PreservaSpanId()
        {
            var spanId = "00f067aa0ba902b7";
            var entry = CriarEntry(spanId: spanId);

            Assert.Equal(spanId, entry.SpanId);
        }

        // ✅ Teste 14: ParentSpanId fornecido → preservado
        [Fact]
        public void BufferedLogEntry_ComParentSpanId_PreservaParentSpanId()
        {
            var parentSpanId = "00f067aa0ba902b6";
            var entry = CriarEntry(parentSpanId: parentSpanId);

            Assert.Equal(parentSpanId, entry.ParentSpanId);
        }

        // ✅ Teste 15: Todos os campos de tracing fornecidos → preservados
        [Fact]
        public void BufferedLogEntry_ComTodosTracing_PreservaTodos()
        {
            var traceId = "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01";
            var spanId = "00f067aa0ba902b7";
            var parentSpanId = "00f067aa0ba902b6";

            var entry = CriarEntry(
                traceId: traceId,
                spanId: spanId,
                parentSpanId: parentSpanId
            );

            Assert.Equal(traceId, entry.TraceId);
            Assert.Equal(spanId, entry.SpanId);
            Assert.Equal(parentSpanId, entry.ParentSpanId);
        }

        // ────────────────────────────────────────
        // Testes de Contexto (ServiceName, Environment)
        // ────────────────────────────────────────

        // ✅ Teste 16: ServiceName e Environment quando não fornecidos → null
        [Fact]
        public void BufferedLogEntry_SemContexto_PropriedadesNull()
        {
            var entry = CriarEntry();

            Assert.Null(entry.ServiceName);
            Assert.Null(entry.Environment);
        }

        // ✅ Teste 17: ServiceName fornecido → preservado
        [Fact]
        public void BufferedLogEntry_ComServiceName_PreservaServiceName()
        {
            var serviceName = "MeuServico";
            var entry = CriarEntry(serviceName: serviceName);

            Assert.Equal(serviceName, entry.ServiceName);
        }

        // ✅ Teste 18: Environment fornecido → preservado
        [Fact]
        public void BufferedLogEntry_ComEnvironment_PreservaEnvironment()
        {
            var environment = "Production";
            var entry = CriarEntry(environment: environment);

            Assert.Equal(environment, entry.Environment);
        }

        // ✅ Teste 19: ServiceName e Environment fornecidos → preservados
        [Fact]
        public void BufferedLogEntry_ComContextoCompleto_PreservaContexto()
        {
            var serviceName = "MeuServico";
            var environment = "Production";

            var entry = CriarEntry(
                serviceName: serviceName,
                environment: environment
            );

            Assert.Equal(serviceName, entry.ServiceName);
            Assert.Equal(environment, entry.Environment);
        }

        // ────────────────────────────────────────
        // Helper
        // ────────────────────────────────────────
        private static BufferedLogEntry CriarEntry(
            DateTimeOffset? timestamp = null,
            string category = "TestCategory",
            LogLevel logLevel = LogLevel.Information,
            EventId? eventId = null,
            string message = "Test message",
            Exception exception = null,
            object state = null,
            IReadOnlyDictionary<string, object> scopes = null,
            string traceId = null,
            string spanId = null,
            string parentSpanId = null,
            string serviceName = null,
            string environment = null)
        {
            return new BufferedLogEntry(
                timestamp: timestamp ?? DateTimeOffset.UtcNow,
                category: category,
                logLevel: logLevel,
                eventId: eventId ?? 1,
                message: message,
                exception: exception,
                state: state,
                scopes: scopes ?? new Dictionary<string, object>(),
                traceId: traceId,
                spanId: spanId,
                parentSpanId: parentSpanId,
                serviceName: serviceName,
                environment: environment
            );
        }
    }
}
