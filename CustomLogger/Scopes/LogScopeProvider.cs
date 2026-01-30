using CustomLogger.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace CustomLogger.Scopes
{
    public sealed class LogScopeProvider : ILogScopeProvider
    {
        private readonly AsyncLocal<Stack<object>> _scopes = new AsyncLocal<Stack<object>>();

        public IDisposable Push(object state)
        {
            if (_scopes.Value == null)
                _scopes.Value = new Stack<object>();

            _scopes.Value.Push(state);

            return new LogScope(() =>
            {
                if (_scopes.Value != null && _scopes.Value.Count > 0)
                    _scopes.Value.Pop();
            });
        }

        /// <summary>
        /// Captura todos os escopos ativos no contexto atual.
        /// REGRA DE COLISÃO: Scope mais interno (último BeginScope) prevalece.
        /// Exemplo:
        ///   using (BeginScope(new { UserId = "123" }))
        ///   using (BeginScope(new { UserId = "456" }))
        ///   // Resultado: UserId = "456"
        /// </summary>
        public IReadOnlyDictionary<string, object> GetScopes()
        {
            var result = new Dictionary<string, object>();

            if (_scopes.Value == null || _scopes.Value.Count == 0)
                return result;

            // ✅ REGRA: Scope mais INTERNO prevalece (último empilhado)
            // Itera do topo da pilha para a base
            foreach (var scope in _scopes.Value)
            {
                if (scope is IEnumerable<KeyValuePair<string, object>> kvs)
                {
                    foreach (var kv in kvs)
                    {
                        // ✅ Só adiciona se chave ainda não existe
                        // Garante que scope interno prevalece
                        if (!result.ContainsKey(kv.Key))
                        {
                            result[kv.Key] = kv.Value;
                        }
                    }
                }
                else
                {
                    // ✅ Scope genérico: usa índice sequencial
                    var key = $"scope_{_scopes.Value.Count - 1}";
                    if (!result.ContainsKey(key))
                    {
                        result[key] = scope;
                    }
                }
            }

            return result;
        }
    }
}

