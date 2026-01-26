using CustomLogger.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace CustomLogger.Scopes
{
    public sealed class LogScopeProvider : ILogScopeProvider
    {
        private static readonly AsyncLocal<Stack<object>> _scopes =
            new AsyncLocal<Stack<object>>();

        public IDisposable Push(object state)
        {
            if (_scopes.Value == null)
                _scopes.Value = new Stack<object>();

            _scopes.Value.Push(state);

            return new Scope(() =>
            {
                _scopes.Value.Pop();
            });
        }

        public IReadOnlyDictionary<string, object> GetScopes()
        {
            var result = new Dictionary<string, object>();

            if (_scopes.Value == null)
                return result;

            foreach (var scope in _scopes.Value)
            {
                if (scope is IEnumerable<KeyValuePair<string, object>> kvs)
                {
                    foreach (var kv in kvs)
                        result[kv.Key] = kv.Value;
                }
                else
                {
                    result["scope"] = scope;
                }
            }

            return result;
        }
    }
}
