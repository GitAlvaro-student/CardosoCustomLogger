using CustomLogger.Abstractions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace CustomLogger.Sinks
{
    public sealed class CompositeLogSink : ILogSink
    {
        private readonly IReadOnlyCollection<ILogSink> _sinks;

        public CompositeLogSink(IEnumerable<ILogSink> sinks)
        {
            _sinks = new List<ILogSink>(sinks);
            Debug.WriteLine($"Total de Sinks: {_sinks.Count}");
        }

        public void Write(ILogEntry entry)
        {
            foreach (var sink in _sinks)
            {
                try
                {
                    sink.Write(entry);
                }
                catch
                {
                    // Falha em um sink não derruba os outros
                }
            }
        }
    }
}
