using System;
using System.Collections.Generic;
using System.Text;

namespace CustomLogger.Abstractions
{
    public interface ILogFormatter
    {
        string Format(ILogEntry logEntry);
    }
}
