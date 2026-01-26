using System;
using System.Collections.Generic;
using System.Text;

namespace CustomLogger.Abstractions
{
    public interface ILogScopeProvider
    {
        IDisposable Push(object state);
        IReadOnlyDictionary<string, object> GetScopes();
    }
}
