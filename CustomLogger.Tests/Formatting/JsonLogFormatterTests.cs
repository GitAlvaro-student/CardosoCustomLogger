using CustomLogger.Buffering;
using CustomLogger.Formatting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

namespace CustomLogger.Tests.Formatting
{
    public sealed class JsonLogFormatterTests
    {
        private static readonly JsonDocumentOptions _jsonOptions = new JsonDocumentOptions
        {
            AllowTrailingCommas = true
        };

        // ────────────────────────────────────────
        // JSON Válido
        // ────────────────────────────────────────

        // ✅ Teste 1: Retorna JSON válido
        [Fact]
        public void Format_RetornaJsonValido()
        {
            var formatter = new JsonLogFormatter();
            var entry = CriarEntry("Mensagem simples");

            var json = formatter.Format(entry);

            // Não deve lançar exceção ao parsear
            using var doc = JsonDocument.Parse(json, _jsonOptions);
            Assert.NotNull(doc.RootElement);
        }

        // ✅ Teste 2: Entry nula → retorna JSON válido
        [Fact]
        public void Format_EntryNula_RetornaJsonValido()
        {
            var formatter = new JsonLogFormatter();

            var json = formatter.Format(null);

            using var doc = JsonDocument.Parse(json, _jsonOptions);
            Assert.NotNull(doc.RootElement);
        }

        // ────────────────────────────────────────
        // Campos Obrigatórios
        // ────────────────────────────────────────

        // ✅ Teste 3: Campos obrigatórios presentes
        [Fact]
        public void Format_CamposObrigatorios_Presentes()
        {
            var formatter = new JsonLogFormatter();
            var entry = CriarEntry("Teste campos");

            var json = formatter.Format(entry);
            var doc = JsonDocument.Parse(json, _jsonOptions);
            var root = doc.RootElement;

            Assert.True(root.TryGetProperty("timestamp", out _));
            Assert.True(root.TryGetProperty("level", out _));
            Assert.True(root.TryGetProperty("category", out _));
            Assert.True(root.TryGetProperty("eventId", out _));
            Assert.True(root.TryGetProperty("message", out _));
            Assert.True(root.TryGetProperty("exception", out _));
            Assert.True(root.TryGetProperty("scopes", out _));
            Assert.True(root.TryGetProperty("state", out _));
        }

        // ✅ Teste 4: Valores dos campos obrigatórios corretos
        [Fact]
        public void Format_ValoresCampos_Corretos()
        {
            var formatter = new JsonLogFormatter();
            var timestamp = DateTimeOffset.UtcNow;
            var eventId = new EventId(42, "TestEvent");

            var entry = CriarEntry(
                message: "Mensagem teste",
                timestamp: timestamp,
                category: "MinhaCategoria",
                logLevel: LogLevel.Warning,
                eventId: eventId
            );

            var json = formatter.Format(entry);
            var doc = JsonDocument.Parse(json, _jsonOptions);
            var root = doc.RootElement;

            Assert.Equal("Warning", root.GetProperty("level").GetString());
            Assert.Equal("MinhaCategoria", root.GetProperty("category").GetString());
            Assert.Equal("Mensagem teste", root.GetProperty("message").GetString());
            Assert.Equal(42, root.GetProperty("eventId").GetInt32());
            Assert.Equal("TestEvent", root.GetProperty("eventName").GetString());
        }

        // ────────────────────────────────────────
        // Mensagens
        // ────────────────────────────────────────

        // ✅ Teste 5: Mensagem simples
        [Fact]
        public void Format_MensagemSimples_Preservada()
        {
            var formatter = new JsonLogFormatter();
            var entry = CriarEntry("Mensagem simples de teste");

            var json = formatter.Format(entry);
            var doc = JsonDocument.Parse(json, _jsonOptions);

            Assert.Equal("Mensagem simples de teste", doc.RootElement.GetProperty("message").GetString());
        }

        // ✅ Teste 6: Mensagem com caracteres especiais
        [Fact]
        public void Format_MensagemComCaracteresEspeciais_Preservada()
        {
            var formatter = new JsonLogFormatter();
            var entry = CriarEntry("Mensagem com \"aspas\" e \\backslash\\ e \nnewline");

            var json = formatter.Format(entry);

            // Deve ser JSON válido mesmo com caracteres especiais
            using var doc = JsonDocument.Parse(json, _jsonOptions);
            Assert.Contains("aspas", doc.RootElement.GetProperty("message").GetString());
        }

        // ✅ Teste 7: Mensagem vazia
        [Fact]
        public void Format_MensagemVazia_JsonValido()
        {
            var formatter = new JsonLogFormatter();
            var entry = CriarEntry("");

            var json = formatter.Format(entry);

            using var doc = JsonDocument.Parse(json, _jsonOptions);
            Assert.Equal("", doc.RootElement.GetProperty("message").GetString());
        }

        // ────────────────────────────────────────
        // Exception
        // ────────────────────────────────────────

        // ✅ Teste 8: Sem exception → campo nulo
        [Fact]
        public void Format_SemException_CampoNulo()
        {
            var formatter = new JsonLogFormatter();
            var entry = CriarEntry("Sem erro", exception: null);

            var json = formatter.Format(entry);
            var doc = JsonDocument.Parse(json, _jsonOptions);

            Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("exception").ValueKind);
        }

        // ✅ Teste 9: Com exception → campos serializados
        [Fact]
        public void Format_ComException_CamposPresentes()
        {
            var formatter = new JsonLogFormatter();
            var exception = new InvalidOperationException("Erro de teste");
            var entry = CriarEntry("Erro ocorreu", exception: exception);

            var json = formatter.Format(entry);
            var doc = JsonDocument.Parse(json, _jsonOptions);
            var exceptionNode = doc.RootElement.GetProperty("exception");

            Assert.True(exceptionNode.TryGetProperty("type", out _));
            Assert.True(exceptionNode.TryGetProperty("message", out _));
            Assert.True(exceptionNode.TryGetProperty("stackTrace", out _));
            Assert.Contains("InvalidOperationException", exceptionNode.GetProperty("type").GetString());
            Assert.Equal("Erro de teste", exceptionNode.GetProperty("message").GetString());
        }

        // ✅ Teste 10: Exception aninhada → InnerException presente
        [Fact]
        public void Format_ExceptionAninhada_InnerExceptionPresente()
        {
            var formatter = new JsonLogFormatter();
            var inner = new ArgumentException("Erro interno");
            var outer = new InvalidOperationException("Erro externo", inner);
            var entry = CriarEntry("Erro aninhado", exception: outer);

            var json = formatter.Format(entry);
            var doc = JsonDocument.Parse(json, _jsonOptions);
            var exceptionNode = doc.RootElement.GetProperty("exception");

            Assert.True(exceptionNode.TryGetProperty("innerException", out _));
            Assert.Equal("Erro interno", exceptionNode.GetProperty("innerException").GetString());
        }

        // ✅ Teste 11: StackTrace limitado a 10 linhas
        [Fact]
        public void Format_StackTraceLimitado()
        {
            var formatter = new JsonLogFormatter();

            // Gera exception com stack trace real
            Exception exception;
            try
            {
                GerarStackTraceProfundo(20);
                exception = null;  // Nunca chega aqui
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            var entry = CriarEntry("Stack trace longo", exception: exception);
            var json = formatter.Format(entry);
            var doc = JsonDocument.Parse(json, _jsonOptions);

            var stackTrace = doc.RootElement
                .GetProperty("exception")
                .GetProperty("stackTrace")
                .GetString();

            var lines = stackTrace.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            Assert.True(lines.Length <= 10);
        }

        // ────────────────────────────────────────
        // Scopes
        // ────────────────────────────────────────

        // ✅ Teste 12: Sem scopes → objeto vazio
        [Fact]
        public void Format_SemScopes_ObjetoVazio()
        {
            var formatter = new JsonLogFormatter();
            var entry = CriarEntry("Sem scopes", scopes: new Dictionary<string, object>());

            var json = formatter.Format(entry);
            var doc = JsonDocument.Parse(json, _jsonOptions);

            Assert.Equal(0, doc.RootElement.GetProperty("scopes").EnumerateObject().Count());
        }

        // ✅ Teste 13: Com scopes → valores preservados
        [Fact]
        public void Format_ComScopes_ValoresPreservados()
        {
            var formatter = new JsonLogFormatter();
            var scopes = new Dictionary<string, object>
            {
                ["RequestId"] = "abc-123",
                ["UserId"] = 42
            };

            var entry = CriarEntry("Com scopes", scopes: scopes);

            var json = formatter.Format(entry);
            var doc = JsonDocument.Parse(json, _jsonOptions);
            var scopesNode = doc.RootElement.GetProperty("scopes");

            Assert.Equal("abc-123", scopesNode.GetProperty("RequestId").GetString());
            Assert.Equal(42, scopesNode.GetProperty("UserId").GetInt32());
        }

        // ────────────────────────────────────────
        // State
        // ────────────────────────────────────────

        // ✅ Teste 14: Sem state → campo nulo
        [Fact]
        public void Format_SemState_CampoNulo()
        {
            var formatter = new JsonLogFormatter();
            var entry = CriarEntry("Sem state", state: null);

            var json = formatter.Format(entry);
            var doc = JsonDocument.Parse(json, _jsonOptions);

            Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("state").ValueKind);
        }

        // ✅ Teste 15: Com state serializável → preservado
        [Fact]
        public void Format_ComStateSerializavel_Preservado()
        {
            var formatter = new JsonLogFormatter();
            var state = new List<KeyValuePair<string, object>>
        {
            new KeyValuePair<string, object>("OrderId", 123),
            new KeyValuePair<string, object>("{OriginalFormat}", "Pedido {OrderId}")
        };

            var entry = CriarEntry("Com state", state: state);

            var json = formatter.Format(entry);
            var doc = JsonDocument.Parse(json, _jsonOptions);

            Assert.NotEqual(JsonValueKind.Null, doc.RootElement.GetProperty("state").ValueKind);
        }

        // ✅ Teste 16: State não-serializável → fallback sem exceção
        [Fact]
        public void Format_StateNaoSerializavel_FallbackSemExcecao()
        {
            var formatter = new JsonLogFormatter();

            // Objeto circular (não serializável)
            var state = new { Connection = new object() };

            var entry = CriarEntry("State inválido", state: state);

            // Não deve lançar exceção
            var json = formatter.Format(entry);

            using var doc = JsonDocument.Parse(json, _jsonOptions);
            Assert.NotNull(doc.RootElement.GetProperty("state"));
        }

        // ────────────────────────────────────────
        // Activity / TraceId
        // ────────────────────────────────────────

        // ✅ Teste 17: Log com Activity ativo → TraceId no scope
        [Fact]
        public void Format_ComActivityAtivo_TraceIdPresente()
        {
            var formatter = new JsonLogFormatter();

            using var activity = new Activity("TestOperation").Start();
            var scopes = new Dictionary<string, object>
            {
                ["TraceId"] = activity.TraceId.ToString(),
                ["SpanId"] = activity.SpanId.ToString(),
                ["ActivityName"] = activity.OperationName
            };

            var entry = CriarEntry("Com activity", scopes: scopes);

            var json = formatter.Format(entry);
            var doc = JsonDocument.Parse(json, _jsonOptions);
            var scopesNode = doc.RootElement.GetProperty("scopes");

            Assert.True(scopesNode.TryGetProperty("TraceId", out _));
            Assert.True(scopesNode.TryGetProperty("SpanId", out _));
            Assert.Equal("TestOperation", scopesNode.GetProperty("ActivityName").GetString());
            Assert.NotEmpty(scopesNode.GetProperty("TraceId").GetString());
            Assert.NotEmpty(scopesNode.GetProperty("SpanId").GetString());
        }

        // ✅ Teste 18: Sem Activity → sem TraceId
        [Fact]
        public void Format_SemActivity_SemTraceId()
        {
            var formatter = new JsonLogFormatter();
            var entry = CriarEntry("Sem activity", scopes: new Dictionary<string, object>());

            var json = formatter.Format(entry);
            var doc = JsonDocument.Parse(json, _jsonOptions);

            Assert.False(doc.RootElement.GetProperty("scopes").TryGetProperty("TraceId", out _));
        }

        // ────────────────────────────────────────
        // Compatibilidade com parsers externos
        // ────────────────────────────────────────

        // ✅ Teste 19: JSON parsável por System.Text.Json
        [Fact]
        public void Format_Parseavel_SystemTextJson()
        {
            var formatter = new JsonLogFormatter();
            var entry = CriarEntry(
                message: "Teste parser",
                exception: new Exception("Erro"),
                scopes: new Dictionary<string, object> { ["Key"] = "Value" },
                state: new List<KeyValuePair<string, object>>
                {
                new KeyValuePair<string, object>("Param", "valor")
                }
            );

            var json = formatter.Format(entry);

            // Deve parsear sem exceção
            using var doc = JsonDocument.Parse(json, _jsonOptions);
            Assert.Equal(JsonValueKind.Object, doc.RootElement.ValueKind);
        }

        // ✅ Teste 20: JSON em uma única linha (sem indentação)
        [Fact]
        public void Format_JsonCompacto_UmaLinha()
        {
            var formatter = new JsonLogFormatter();
            var entry = CriarEntry("Compacto");

            var json = formatter.Format(entry);

            // JSON compacto não tem quebras de linha
            Assert.DoesNotContain("\n", json);
        }

        // ────────────────────────────────────────
        // Helpers
        // ────────────────────────────────────────
        private static void GerarStackTraceProfundo(int profundidade)
        {
            if (profundidade <= 0)
                throw new Exception("Stack trace profundo");

            GerarStackTraceProfundo(profundidade - 1);
        }

        private static BufferedLogEntry CriarEntry(
            string message = "Test",
            DateTimeOffset? timestamp = null,
            string category = "TestCategory",
            LogLevel logLevel = LogLevel.Information,
            EventId? eventId = null,
            Exception exception = null,
            object state = null,
            IReadOnlyDictionary<string, object> scopes = null)
        {
            return new BufferedLogEntry(
                timestamp: timestamp ?? DateTimeOffset.UtcNow,
                category: category,
                logLevel: logLevel,
                eventId: eventId ?? 1,
                message: message,
                exception: exception,
                state: state,
                scopes: scopes ?? new Dictionary<string, object>()
            );
        }
    }
}
