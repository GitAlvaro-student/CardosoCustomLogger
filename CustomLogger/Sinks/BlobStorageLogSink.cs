using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using CustomLogger.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CustomLogger.Sinks
{
    public sealed class BlobStorageLogSink : ILogSink
    {
        private readonly ILogFormatter _formatter;
        private readonly AppendBlobClient _blobClient;

        public BlobStorageLogSink(
            string connectionString,
            string containerName,
            string blobName,
            ILogFormatter formatter)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException(nameof(connectionString));

            if (string.IsNullOrWhiteSpace(containerName))
                throw new ArgumentException(nameof(containerName));

            if (string.IsNullOrWhiteSpace(blobName))
                throw new ArgumentException(nameof(blobName));

            _formatter = formatter
                ?? throw new ArgumentNullException(nameof(formatter));

            var containerClient = new BlobContainerClient(
                connectionString,
                containerName);

            containerClient.CreateIfNotExists();

            var appendBlobClient =
                containerClient.GetAppendBlobClient(blobName);

            appendBlobClient.CreateIfNotExists();

            _blobClient = appendBlobClient;
        }

        public void Write(ILogEntry entry)
        {
            if (entry == null)
                return;

            var json = _formatter.Format(entry) + Environment.NewLine;
            var bytes = Encoding.UTF8.GetBytes(json);

            using (var stream = new MemoryStream(bytes))
            {
                _blobClient.AppendBlock(stream);
            }
        }
    }
}
