using System;
using System.Collections.Generic;
using System.Text;

namespace CustomLogger.Abstractions
{
    public interface ILogBuffer
    {
        void Enqueue(ILogEntry entry);
        void Flush();
    }
}
