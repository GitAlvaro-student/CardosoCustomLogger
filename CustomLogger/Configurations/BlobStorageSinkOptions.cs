using System;
using System.Collections.Generic;
using System.Text;

namespace CustomLogger.Configurations
{
    public sealed class BlobStorageSinkOptions
    {
        public string ConnectionString { get; set; }
        public string ContainerName { get; set; }
        public string BlobName { get; set; }
    }
}
