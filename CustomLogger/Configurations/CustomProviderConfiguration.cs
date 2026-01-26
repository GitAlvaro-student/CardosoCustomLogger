using System;
using System.Collections.Generic;
using System.Text;

namespace CustomLogger.Configurations
{
    /// <summary>
    /// Representa a configuração efetiva do Custom Logger Provider
    /// em tempo de execução.
    /// </summary>
    public sealed class CustomProviderConfiguration
    {
        /// <summary>
        /// Opções configuradas para o provider.
        /// </summary>
        public CustomProviderOptions Options { get; }

        public CustomProviderConfiguration(CustomProviderOptions options)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));
        }
    }
}
