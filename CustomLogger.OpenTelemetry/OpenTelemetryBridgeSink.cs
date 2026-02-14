using CustomLogger.Abstractions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace CustomLogger.OpenTelemetry
{
    /// <summary>
    /// Sink que faz bridge entre CustomLogger e OpenTelemetry.
    /// Converte ILogEntry em eventos de Activity do OpenTelemetry.
    /// </summary>
    public sealed class OpenTelemetryBridgeSink : ILogSink
    {
        /// <summary>
        /// Escreve uma entrada de log para o OpenTelemetry.
        /// Se Activity.Current existir, adiciona um evento com a mensagem do log.
        /// </summary>
        /// <param name="entry">Entrada de log.</param>
        public void Write(ILogEntry entry)
        {
            var activity = Activity.Current;

            if (activity != null)
            {
                activity.AddEvent(new ActivityEvent(entry.Message));
            }
        }
    }
}
