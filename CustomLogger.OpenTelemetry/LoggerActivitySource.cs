using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace CustomLogger.OpenTelemetry
{
    /// <summary>
    /// Fonte centralizada de Activities para o CustomLogger.
    /// Garante que todas as Activities sejam criadas a partir da mesma fonte.
    /// </summary>
    public static class LoggerActivitySource
    {
        /// <summary>
        /// ActivitySource compartilhado para toda a biblioteca de logging.
        /// Nome: "CustomLogger"
        /// </summary>
        public static readonly ActivitySource Source = new ActivitySource("CustomLogger");
    }
}
