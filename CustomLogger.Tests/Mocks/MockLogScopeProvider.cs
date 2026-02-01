using CustomLogger.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomLogger.Tests.Mocks
{
    public sealed class MockLogScopeProvider : ILogScopeProvider
    {
        private readonly IReadOnlyDictionary<string, object> _scopes;

        public MockLogScopeProvider(IReadOnlyDictionary<string, object> scopes = null)
        {
            _scopes = scopes ?? new Dictionary<string, object>();
        }

        public IDisposable Push(object state)
        {
            return NoOpDisposable.Instance;  // Não faz nada
        }

        public IReadOnlyDictionary<string, object> GetScopes()
        {
            return _scopes;
        }

        internal sealed class NoOpDisposable : IDisposable
        {
            public static readonly NoOpDisposable Instance = new();

            private NoOpDisposable() { }

            public void Dispose()
            {
                // intencionalmente vazio
            }
        }

    }
}
