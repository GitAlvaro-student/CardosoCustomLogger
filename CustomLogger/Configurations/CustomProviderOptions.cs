using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;

namespace CustomLogger.Configurations
{
    /// <summary>
    /// Define as opções de configuração do Custom Logger Provider.
    /// Esta classe é um POCO e deve conter apenas estado.
    /// </summary>
    public class CustomProviderOptions
    {
        /// <summary>
        /// Nível mínimo de log permitido.
        /// Logs abaixo desse nível serão ignorados.
        /// </summary>
        public LogLevel MinimumLogLevel { get; set; } = LogLevel.Information;

        /// <summary>
        /// Indica se o buffer global de logs deve ser utilizado.
        /// </summary>
        public bool UseGlobalBuffer { get; set; } = true;

        /// <summary>
        /// Número máximo de logs mantidos no buffer antes do flush.
        /// </summary>
        public int MaxBufferSize { get; set; } = 1000;

        /// <summary>
        /// Intervalo máximo para descarregar o buffer.
        /// Pode ser utilizado por timers externos.
        /// </summary>
        public TimeSpan FlushInterval { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Indica se exceções devem ser sempre registradas,
        /// independentemente do nível de log.
        /// </summary>
        public bool AlwaysLogExceptions { get; set; } = true;

        /// <summary>
        /// Indica se a Fila deve ser descartada quando o buffer estiver cheio.
        /// </summary>
        public bool DropOldestOnOverflow { get; set; } = true;

        /// <summary>
        /// Indica se Fallback Sink deve ser habilitado.
        /// </summary>
        public bool EnableFallbackSink { get; set; } = true;
    }
}
