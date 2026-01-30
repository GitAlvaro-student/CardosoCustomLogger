using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CustomLogger.Abstractions
{
    public interface IAsyncLogSink : ILogSink
    {
        Task WriteAsync(ILogEntry entry, CancellationToken cancellationToken = default);
    }
}
