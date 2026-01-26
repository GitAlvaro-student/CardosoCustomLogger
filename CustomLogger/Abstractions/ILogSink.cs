using System;
using System.Collections.Generic;
using System.Text;

namespace CustomLogger.Abstractions
{
    /// <summary>
    /// Destino final de logs.
    /// </summary>
    public interface ILogSink
    {
        void Write(ILogEntry entry);
    }
}
