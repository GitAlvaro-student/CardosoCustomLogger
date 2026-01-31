using System;
using System.Collections.Generic;
using System.Text;

namespace CustomLogger.Configurations
{
    public sealed class BatchOptions
    {
        public int BatchSize { get; set; } = 50;
        public TimeSpan FlushInterval { get; set; } = TimeSpan.FromSeconds(10);
    }

}
