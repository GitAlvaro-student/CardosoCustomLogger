using CustomLogger.Abstractions;
using CustomLogger.Configurations;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace CustomLogger.Buffering
{
    public sealed class InstanceLogBuffer : ILogBuffer
    {
        private readonly ILogSink _sink;
        private readonly ConcurrentQueue<ILogEntry> _queue = new ConcurrentQueue<ILogEntry>();
        private readonly CustomProviderOptions _options;

        public InstanceLogBuffer(ILogSink sink, CustomProviderOptions options)
        {
            _sink = sink ?? throw new ArgumentNullException(nameof(sink));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public void Enqueue(ILogEntry entry)
        {
            if (entry == null) return;

            if (!_options.UseGlobalBuffer)
            {
                _sink.Write(entry);
                return;
            }

            _queue.Enqueue(entry);

            if (_queue.Count >= _options.MaxBufferSize)
            {
                Flush();
            }
        }

        public void Flush()
        {
            Debug.WriteLine($"[DEBUG] Flush chamado. Itens na fila: {_queue.Count}");

            while (_queue.TryDequeue(out var entry))
            {
                Debug.WriteLine($"[DEBUG] Processando log: {entry.Message}");

                try
                {
                    _sink.Write(entry);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[DEBUG] Erro no sink: {ex.Message}");
                }
            }

            Debug.WriteLine("[DEBUG] Flush concluído");
        }
    }
}
