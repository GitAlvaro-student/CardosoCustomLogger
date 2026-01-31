using System;
using System.Collections.Generic;
using System.Text;

namespace CustomLogger.Abstractions
{
    // IBatchLogSink.cs
    public interface IBatchLogSink : ILogSink
    {
        void WriteBatch(IEnumerable<ILogEntry> entries);
    }
}
