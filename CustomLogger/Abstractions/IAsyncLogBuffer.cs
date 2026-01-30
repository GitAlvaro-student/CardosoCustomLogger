using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CustomLogger.Abstractions
{
    public interface IAsyncLogBuffer : ILogBuffer
    {
        Task EnqueueAsync(ILogEntry entry, CancellationToken cancellationToken = default);
        Task FlushAsync(CancellationToken cancellationToken = default);
    }
}
