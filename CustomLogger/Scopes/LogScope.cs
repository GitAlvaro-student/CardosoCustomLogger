using System;
using System.Collections.Generic;
using System.Text;

namespace CustomLogger.Scopes
{

    public sealed class Scope : IDisposable
    {
        private readonly Action _onDispose;

        public Scope(Action onDispose)
        {
            _onDispose = onDispose;
        }

        public void Dispose()
        {
            _onDispose();
        }
    }
}
