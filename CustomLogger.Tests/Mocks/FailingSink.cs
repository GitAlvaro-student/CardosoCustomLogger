using CustomLogger.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomLogger.Tests.Models
{
    public sealed class FailingSink : ILogSink
    {
        public void Write(ILogEntry entry)
        {
            throw new Exception("Sink falhou");
        }
    }

}
