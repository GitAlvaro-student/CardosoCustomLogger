using CustomLogger.Abstractions;
using CustomLogger.Configurations;
using System;
using System.Collections.Generic;
using System.Text;

namespace CustomLogger.Buffering
{
    /// <summary>
    /// Adaptador do buffer global para o contrato ILogBuffer.
    /// </summary>
    /// <summary>
    /// Adaptador que expõe o GlobalLogBuffer via ILogBuffer.
    /// </summary>
    public sealed class GlobalLogBufferAdapter : ILogBuffer
    {
        private readonly CustomProviderConfiguration _configuration;

        public GlobalLogBufferAdapter(CustomProviderConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void Enqueue(ILogEntry entry)
        {
            if (entry is BufferedLogEntry bufferedEntry)
            {
                GlobalLogBuffer.Enqueue(bufferedEntry, _configuration);
            }
        }

        public void Flush()
        {
            GlobalLogBuffer.Flush();
        }
    }
}
