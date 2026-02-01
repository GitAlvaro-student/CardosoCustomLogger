using CustomLogger.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace CustomLogger.Sinks
{
    public sealed class FailingSink : ILogSink
    {
        public void Write(ILogEntry entry)
        {
            throw new Exception("Sink falhou");
        }
    }
}
