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

        public BatchOptions BatchOptions { get; set; } = new BatchOptions();

        public BackpressureOptions BackpressureOptions { get; set; } = new BackpressureOptions();
    }
}
