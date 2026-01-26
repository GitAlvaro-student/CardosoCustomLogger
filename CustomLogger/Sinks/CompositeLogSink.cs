using CustomLogger.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace CustomLogger.Sinks
{
    public sealed class CompositeLogSink : ILogSink
    {
        private readonly IReadOnlyCollection<ILogSink> _sinks;

        public CompositeLogSink(IEnumerable<ILogSink> sinks)
        {
            _sinks = new List<ILogSink>(sinks);
        }

        public void Write(ILogEntry entry)
        {
            foreach (var sink in _sinks)
            {
                sink.Write(entry);
            }
        }
    }
}
