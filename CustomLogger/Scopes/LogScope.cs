using System;
using System.Collections.Generic;
using System.Text;

namespace CustomLogger.Scopes
{

    public sealed class LogScope : IDisposable
    {
        private readonly Action _dispose;
        private bool _disposed;

        public LogScope(Action dispose)
        {
            _dispose = dispose;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _dispose?.Invoke();
        }
    }
}
