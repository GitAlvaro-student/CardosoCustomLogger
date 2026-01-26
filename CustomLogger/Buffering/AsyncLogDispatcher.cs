using CustomLogger.Abstractions;
using CustomLogger.Configurations;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CustomLogger.Buffering
{
    public sealed class AsyncLogDispatcher : IDisposable
    {
        private readonly BlockingCollection<BufferedLogEntry> _queue;
        private readonly ILogSink _sink;
        private readonly List<BufferedLogEntry> _batch;
        private readonly BatchOptions _options;

        private readonly Task _worker;
        private readonly Timer _timer;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public AsyncLogDispatcher(
            ILogSink sink,
            BatchOptions options,
            int capacity = 10_000)
        {
            _sink = sink;
            _options = options;

            _queue = new BlockingCollection<BufferedLogEntry>(capacity);
            _batch = new List<BufferedLogEntry>(_options.BatchSize);

            _worker = Task.Factory.StartNew(
                ProcessQueue,
                TaskCreationOptions.LongRunning);

            _timer = new Timer(
                _ => FlushBatch(),
                null,
                _options.FlushInterval,
                _options.FlushInterval);
        }

        public void Enqueue(BufferedLogEntry entry)
        {
            _queue.TryAdd(entry);
        }

        private void ProcessQueue()
        {
            try
            {
                foreach (var entry in _queue.GetConsumingEnumerable(_cts.Token))
                {
                    lock (_batch)
                    {
                        _batch.Add(entry);

                        if (_batch.Count >= _options.BatchSize)
                        {
                            FlushBatchUnsafe();
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // shutdown
            }
        }

        private void FlushBatch()
        {
            lock (_batch)
            {
                FlushBatchUnsafe();
            }
        }

        private void FlushBatchUnsafe()
        {
            if (_batch.Count == 0)
                return;

            foreach (var entry in _batch)
            {
                _sink.Write(entry);
            }

            _batch.Clear();
        }

        public void Dispose()
        {
            _queue.CompleteAdding();
            _cts.Cancel();

            _worker.Wait();

            FlushBatch();
            _timer.Dispose();
        }
    }


}
