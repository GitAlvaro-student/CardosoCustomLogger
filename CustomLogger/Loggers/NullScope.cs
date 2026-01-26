using System;
using System.Collections.Generic;
using System.Text;

namespace CustomLogger.Loggers
{
    /// <summary>
    /// Implementação padrão de escopo nulo.
    /// Compatível com .NET Standard 2.0.
    /// </summary>
    public sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new NullScope();

        private NullScope() { }

        public void Dispose() { }
    }
}
