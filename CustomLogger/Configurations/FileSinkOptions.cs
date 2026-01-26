using System;
using System.Collections.Generic;
using System.Text;

namespace CustomLogger.Configurations
{
    public sealed class FileSinkOptions
    {
        public string FilePath { get; set; }
        public bool Append { get; set; } = true;
    }
}
