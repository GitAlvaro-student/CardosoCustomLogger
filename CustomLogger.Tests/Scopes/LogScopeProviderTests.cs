using CustomLogger.Scopes;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CustomLogger.Tests.Scopes
{
    public sealed class LogScopeProviderTests
    {
        // ────────────────────────────────────────
        // Escopo Único
        // ────────────────────────────────────────

        // ✅ Teste 1: Sem scope → Scopes vazio
        [Fact]
        public void GetScopes_SemScope_RetornaVazio()
        {
            var provider = new LogScopeProvider();

            var scopes = provider.GetScopes();

            Assert.Empty(scopes);
        }

        // ✅ Teste 2: Um scope → preservado
        [Fact]
        public void Push_EscopoUnico_Preservado()
        {
            var provider = new LogScopeProvider();

            using (provider.Push(new Dictionary<string, object>
            {
                ["RequestId"] = "abc-123"
            }))
            {
                var scopes = provider.GetScopes();

                Assert.Single(scopes);
                Assert.Equal("abc-123", scopes["RequestId"]);
            }
        }

        // ✅ Teste 3: Scope com múltiplas chaves
        [Fact]
        public void Push_MultipleChaves_TodasPreservadas()
        {
            var provider = new LogScopeProvider();

            using (provider.Push(new Dictionary<string, object>
            {
                ["RequestId"] = "abc-123",
                ["UserId"] = 42,
                ["Feature"] = "checkout"
            }))
            {
                var scopes = provider.GetScopes();

                Assert.Equal(3, scopes.Count);
                Assert.Equal("abc-123", scopes["RequestId"]);
                Assert.Equal(42, scopes["UserId"]);
                Assert.Equal("checkout", scopes["Feature"]);
            }
        }

        // ────────────────────────────────────────
        // Dispose
        // ────────────────────────────────────────

        // ✅ Teste 4: Scope removido após Dispose
        [Fact]
        public void Push_AposDispose_ScopeRemovido()
        {
            var provider = new LogScopeProvider();

            using (provider.Push(new Dictionary<string, object>
            {
                ["RequestId"] = "abc-123"
            }))
            {
                Assert.NotEmpty(provider.GetScopes());
            }

            // Após using → Dispose → scope removido
            Assert.Empty(provider.GetScopes());
        }

        // ✅ Teste 5: Dispose duplo → não falha
        [Fact]
        public void Push_DisposeDuplo_NaoFalha()
        {
            var provider = new LogScopeProvider();

            var scope = provider.Push(new Dictionary<string, object>
            {
                ["RequestId"] = "abc-123"
            });

            scope.Dispose();
            scope.Dispose();  // Não deve lançar exceção

            Assert.Empty(provider.GetScopes());
        }

        // ────────────────────────────────────────
        // Escopos Aninhados
        // ────────────────────────────────────────

        // ✅ Teste 6: Dois escopos aninhados → ambos visíveis
        [Fact]
        public void Push_EscoposAninhados_AmbosVisíveis()
        {
            var provider = new LogScopeProvider();

            using (provider.Push(new Dictionary<string, object>
            {
                ["RequestId"] = "abc-123"
            }))
            {
                using (provider.Push(new Dictionary<string, object>
                {
                    ["UserId"] = 42
                }))
                {
                    var scopes = provider.GetScopes();

                    Assert.Equal(2, scopes.Count);
                    Assert.Equal("abc-123", scopes["RequestId"]);
                    Assert.Equal(42, scopes["UserId"]);
                }
            }
        }

        // ✅ Teste 7: Três escopos aninhados → todos visíveis
        [Fact]
        public void Push_TresEscoposAninhados_TodosVisíveis()
        {
            var provider = new LogScopeProvider();

            using (provider.Push(new Dictionary<string, object> { ["Layer"] = "API" }))
            using (provider.Push(new Dictionary<string, object> { ["Service"] = "Orders" }))
            using (provider.Push(new Dictionary<string, object> { ["Method"] = "POST" }))
            {
                var scopes = provider.GetScopes();

                Assert.Equal(3, scopes.Count);
                Assert.Equal("API", scopes["Layer"]);
                Assert.Equal("Orders", scopes["Service"]);
                Assert.Equal("POST", scopes["Method"]);
            }
        }

        // ✅ Teste 8: Scope interno removido → externo permanece
        [Fact]
        public void Push_ScopeInternoRemovido_ExternoPermane()
        {
            var provider = new LogScopeProvider();

            using (provider.Push(new Dictionary<string, object>
            {
                ["RequestId"] = "abc-123"
            }))
            {
                using (provider.Push(new Dictionary<string, object>
                {
                    ["UserId"] = 42
                }))
                {
                    Assert.Equal(2, provider.GetScopes().Count);
                }

                // Interno removido, externo permanece
                var scopes = provider.GetScopes();
                Assert.Single(scopes);
                Assert.Equal("abc-123", scopes["RequestId"]);
            }
        }

        // ────────────────────────────────────────
        // Colisão de Chaves
        // ────────────────────────────────────────

        // ✅ Teste 9: Chave duplicada → interno prevalece
        [Fact]
        public void Push_ChaveDuplicada_InternoPrevalece()
        {
            var provider = new LogScopeProvider();

            using (provider.Push(new Dictionary<string, object>
            {
                ["UserId"] = "externo"
            }))
            {
                using (provider.Push(new Dictionary<string, object>
                {
                    ["UserId"] = "interno"
                }))
                {
                    var scopes = provider.GetScopes();

                    // Interno prevalece (regra de colisão)
                    Assert.Single(scopes);
                    Assert.Equal("interno", scopes["UserId"]);
                }
            }
        }

        // ✅ Teste 10: Chave duplicada após interno removido → externo volta
        [Fact]
        public void Push_ChaveDuplicada_AposInternoRemovido_ExternoVolta()
        {
            var provider = new LogScopeProvider();

            using (provider.Push(new Dictionary<string, object>
            {
                ["UserId"] = "externo"
            }))
            {
                using (provider.Push(new Dictionary<string, object>
                {
                    ["UserId"] = "interno"
                }))
                {
                    Assert.Equal("interno", provider.GetScopes()["UserId"]);
                }

                // Interno removido → externo volta
                Assert.Equal("externo", provider.GetScopes()["UserId"]);
            }
        }

        // ────────────────────────────────────────
        // Scopes Genéricos
        // ────────────────────────────────────────

        // ✅ Teste 11: Scope genérico (não-Dictionary) → chave automática
        [Fact]
        public void Push_ScopeGenerico_ChaveAutomatica()
        {
            var provider = new LogScopeProvider();

            using (provider.Push("scope-string-simples"))
            {
                var scopes = provider.GetScopes();

                Assert.Single(scopes);
                Assert.Contains("scope_", scopes.Keys.First());
                Assert.Equal("scope-string-simples", scopes.Values.First());
            }
        }

        // ✅ Teste 12: Múltiplos scopes genéricos → chaves únicas
        [Fact]
        public void Push_MultiplosScopesGenericos_ChavesUnicas()
        {
            var provider = new LogScopeProvider();

            using (provider.Push("primeiro"))
            using (provider.Push("segundo"))
            {
                var scopes = provider.GetScopes();

                Assert.Equal(2, scopes.Count);
                Assert.Equal(2, scopes.Keys.Distinct().Count());  // Chaves únicas
            }
        }

        // ────────────────────────────────────────
        // Isolamento entre instâncias
        // ────────────────────────────────────────

        // ✅ Teste 13: Instâncias independentes → sem compartilhamento
        [Fact]
        public void Push_InstanciasIndependentes_SemCompartilhamento()
        {
            var provider1 = new LogScopeProvider();
            var provider2 = new LogScopeProvider();

            using (provider1.Push(new Dictionary<string, object> { ["Source"] = "Provider1" }))
            {
                // Provider2 não deve ver scopes do Provider1
                Assert.Empty(provider2.GetScopes());
                Assert.Single(provider1.GetScopes());
            }
        }

        // ✅ Teste 14: Scope nulo → não falha
        [Fact]
        public void Push_ScopeNulo_NaoFalha()
        {
            var provider = new LogScopeProvider();

            using (provider.Push(null))
            {
                // Não deve lançar exceção
                var scopes = provider.GetScopes();
            }
        }

        // ────────────────────────────────────────
        // Correlação em contexto assíncrono
        // ────────────────────────────────────────

        // ✅ Teste 15: Scope preservado em Task
        [Fact]
        public async Task Push_EmTask_ScopePreservado()
        {
            var provider = new LogScopeProvider();

            using (provider.Push(new Dictionary<string, object>
            {
                ["RequestId"] = "async-123"
            }))
            {
                var scopesInTask = await Task.Run(() => provider.GetScopes());

                Assert.Equal("async-123", scopesInTask["RequestId"]);
            }
        }

        // ✅ Teste 16: Tasks paralelas → scopes isolados
        [Fact]
        public async Task Push_TasksParalelas_ScopesIsolados()
        {
            var provider = new LogScopeProvider();
            var results = new Dictionary<string, IReadOnlyDictionary<string, object>>();

            var taskA = Task.Run(() =>
            {
                using (provider.Push(new Dictionary<string, object> { ["TaskId"] = "A" }))
                {
                    // Pequena espera para garantir concorrência
                    Task.Delay(10).Wait();
                    return provider.GetScopes();
                }
            });

            var taskB = Task.Run(() =>
            {
                using (provider.Push(new Dictionary<string, object> { ["TaskId"] = "B" }))
                {
                    Task.Delay(10).Wait();
                    return provider.GetScopes();
                }
            });

            var scopesA = await taskA;
            var scopesB = await taskB;

            // Cada task vê apenas seu próprio scope
            Assert.Equal("A", scopesA["TaskId"]);
            Assert.Equal("B", scopesB["TaskId"]);
        }
    }
}
