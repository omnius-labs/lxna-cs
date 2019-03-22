using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Omnix.Base;
using Omnix.Network;
using Omnix.Serialization;

namespace Lxna.Rpc.Internal
{
    sealed class MessagingManager : DisposableBase
    {
        private INonblockingConnection _connection;
        private TaskManager _taskManager;
        private EventScheduler _aliveTimer;

        private readonly object _lockObject = new object();
        private volatile bool _disposed;

        public MessagingManager(INonblockingConnection connection)
        {
            _connection = connection;
            _taskManager = new TaskManager(this.EventLoop);
            _aliveTimer = new EventScheduler(() => this.Alive());
        }

        public event Action<Stream> ReceiveEvent;

        private void OnReceive(Stream stream)
        {
            this.ReceiveEvent?.Invoke(stream);
        }

        public async ValueTask Run()
        {
            await _taskManager.Start();
            _aliveTimer.Start(1000 * 30);
        }

        public async ValueTask Stop()
        {
            _aliveTimer.Stop();
            await _taskManager.Stop();
        }

        private void EventLoop(CancellationToken token)
        {
            try
            {
                for (; ; )
                {
                    token.ThrowIfCancellationRequested();

                    long length = (long)Varint.GetUInt64(_stream);
                    if (length == 0) continue;

                    Stream resultSteram = null;

                    try
                    {
                        resultSteram = new RecyclableMemoryStream(_bufferManager);

                        using (var safeBuffer = _bufferManager.CreateSafeBuffer(1024 * 32))
                        {
                            long remain = length;

                            while (remain > 0)
                            {
                                int readLength = _stream.Read(safeBuffer.Value, 0, (int)Math.Min(remain, safeBuffer.Value.Length));
                                resultSteram.Write(safeBuffer.Value, 0, readLength);

                                remain -= readLength;
                            }
                        }

                        resultSteram.Seek(0, SeekOrigin.Begin);

                        this.OnReceive(resultSteram);
                    }
                    catch (Exception)
                    {
                        if (resultSteram != null)
                        {
                            resultSteram.Dispose();
                            resultSteram = null;
                        }
                    }
                }
            }
            catch (Exception)
            {

            }
        }

        private void Alive()
        {
            lock (_lockObject)
            {
                Varint.SetUInt64(_stream, 0);
            }
        }

        public void Send(Stream resultStream)
        {
            lock (_lockObject)
            {
                _connection.EnqueueAsync()
                try
                {
                    Varint.SetUInt64(_stream, (ulong)resultStream.Length);

                    using (var safeBuffer = _bufferManager.CreateSafeBuffer(1024 * 32))
                    {
                        long remain = resultStream.Length;

                        while (remain > 0)
                        {
                            int readLength = resultStream.Read(safeBuffer.Value, 0, (int)Math.Min(remain, safeBuffer.Value.Length));
                            _stream.Write(safeBuffer.Value, 0, readLength);
                            _stream.Flush();

                            remain -= readLength;
                        }
                    }
                }
                finally
                {
                    resultStream.Dispose();
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;

            if (disposing)
            {
                if (_aliveTimer != null)
                {
                    _aliveTimer.Dispose();
                    _aliveTimer = null;
                }

                if (_taskManager != null)
                {
                    _taskManager.Dispose();
                    _taskManager = null;
                }
            }
        }
    }
}
