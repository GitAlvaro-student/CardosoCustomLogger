using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CustomLogger.Abstractions
{
    public interface IAsyncBatchLogSink : IBatchLogSink, IAsyncLogSink
    {
        Task WriteBatchAsync(IEnumerable<ILogEntry> entries, CancellationToken cancellationToken = default);
    }
}
