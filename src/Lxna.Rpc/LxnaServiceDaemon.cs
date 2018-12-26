using System;
using System.Buffers;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Lxna.Messages;
using Omnix.Base;
using Omnix.Base.Extensions;
using Omnix.Collections;
using Omnix.Configuration;
using Omnix.Network;
using Omnix.Serialization;
using Omnix.Serialization.RocketPack;

namespace Lxna.Rpc
{
    public sealed class LxnaServiceDaemon : DisposableBase
    {
        private static NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        private INonblockingConnection _connection;
        private ILxnaService _service;

        private LockedHashDictionary<uint, TaskManager> _responseTaskManagers = new LockedHashDictionary<uint, TaskManager>();
        private TaskManager _receiveTaskManager;

        private readonly object _lockObject = new object();
        private volatile bool _disposed;

        public LxnaServiceDaemon(ILxnaService service, INonblockingConnection connection)
        {
            _service = service;
            _connection = connection;

            _receiveTaskManager = new TaskManager((token) => this.ReceiveThread(token));
        }

        public async ValueTask Watch()
        {
            // 受信開始
            _receiveTaskManager.Start();

            // 受信終了まで待機
            await _receiveTaskManager.Task;

            // すべてのレスポンス処理をキャンセル
            foreach (var responseTask in _responseTaskManagers.Values)
            {
                responseTask.Cancel();
            }

            // すべてのレスポンス処理の終了まで待機
            await Task.WhenAll(_responseTaskManagers.Select(n => n.Value.Task).ToArray());
        }

        private void ReceiveThread(CancellationToken receiveToken)
        {
            while (!receiveToken.IsCancellationRequested)
            {
                _connection.DequeueAsync((sequence) =>
                {
                    try
                    {
                        var reader = new RocketPackReader(sequence, BufferPool.Shared);
                        var header = LxnaRpcRequestHeader.Formatter.Deserialize(reader, 0);

                        if (header.Type == LxnaRpcRequestType.Exit)
                        {
                            this.SendResponse(LxnaRpcResponseType.Result, header.Id);
                            _receiveTaskManager.Cancel();
                        }
                        else if (header.Type == LxnaRpcRequestType.Cancel)
                        {
                            if (_responseTaskManagers.TryGetValue(header.Id, out var responseTask))
                            {
                                responseTask.Cancel();
                            }
                        }
                        else
                        {
                            TaskManager taskManager = null;

                            switch (header.Type)
                            {
                                case LxnaRpcRequestType.Load:
                                    {
                                        taskManager = this.CreateResponseTaskManager(header.Id, (responseToken) =>
                                        {
                                            _service.Load();
                                        });
                                        break;
                                    }
                                case LxnaRpcRequestType.Save:
                                    {
                                        taskManager = this.CreateResponseTaskManager(header.Id, (responseToken) =>
                                        {
                                            _service.Save();
                                        });
                                        break;
                                    }
                                case LxnaRpcRequestType.Start:
                                    {
                                        taskManager = this.CreateResponseTaskManager(header.Id, (responseToken) =>
                                        {
                                            _service.Start();
                                        });
                                        break;
                                    }
                                case LxnaRpcRequestType.Stop:
                                    {
                                        taskManager = this.CreateResponseTaskManager(header.Id, (responseToken) =>
                                        {
                                            _service.Stop();
                                        });
                                        break;
                                    }
                                case LxnaRpcRequestType.GetFileMetadatas:
                                    {
                                        var requestBody = GetFileMetadatasRequestBody.Formatter.Deserialize(reader, 0);

                                        taskManager = this.CreateResponseTaskManager(header.Id, (responseToken) =>
                                        {
                                            var result = _service.GetFileMetadatas(requestBody.Path, responseToken);
                                            return new GetFileMetadatasResponseBody(result.ToArray());
                                        });
                                        break;
                                    }
                                default:
                                    throw new NotSupportedException($"LxnaRpcRequestType: ({header.Type})");
                            }

                            _responseTaskManagers.Add(header.Id, taskManager);
                            taskManager.Start();
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.Error(e);
                    }
                }, receiveToken).AsTask().Wait();
            }
        }

        private TaskManager CreateResponseTaskManager(uint id, Action<CancellationToken> callback)
        {
            var taskManager = new TaskManager((token) =>
            {
                try
                {
                    callback.Invoke(token);
                    this.SendResponse(LxnaRpcResponseType.Result, id);
                }
                catch (OperationCanceledException)
                {
                    this.SendResponse(LxnaRpcResponseType.Cancel, id);
                }
                catch (Exception e)
                {
                    this.SendResponse(LxnaRpcResponseType.Error, id, new ErrorMessage(e.GetType().ToString(), e.Message, e.StackTrace?.ToString()));
                    _logger.Error(e);
                }
            });
            return taskManager;
        }

        private TaskManager CreateResponseTaskManager<T>(uint id, Func<CancellationToken, T> callback)
            where T : RocketPackMessageBase<T>
        {
            var taskManager = new TaskManager((token) =>
            {
                try
                {
                    var result = callback.Invoke(token);
                    this.SendResponse(LxnaRpcResponseType.Result, id, result);
                }
                catch (OperationCanceledException)
                {
                    this.SendResponse(LxnaRpcResponseType.Cancel, id);
                }
                catch (Exception e)
                {
                    this.SendResponse(LxnaRpcResponseType.Error, id, new ErrorMessage(e.GetType().ToString(), e.Message, e.StackTrace?.ToString()));
                    _logger.Error(e);
                }
            });
            return taskManager;
        }

        private void SendResponse(LxnaRpcResponseType type, uint id)
        {
            _connection.EnqueueAsync((bufferWriter) =>
            {
                var writer = new RocketPackWriter(bufferWriter, BufferPool.Shared);

                // Headerの書き込み
                var header = new LxnaRpcResponseHeader(type, id);
                LxnaRpcResponseHeader.Formatter.Serialize(writer, header, 0);
            }).AsTask().Wait();
        }

        private void SendResponse<T>(LxnaRpcResponseType type, uint id, T value)
            where T : RocketPackMessageBase<T>
        {
            _connection.EnqueueAsync((bufferWriter) =>
            {
                var writer = new RocketPackWriter(bufferWriter, BufferPool.Shared);

                // Headerの書き込み
                var header = new LxnaRpcResponseHeader(type, id);
                LxnaRpcResponseHeader.Formatter.Serialize(writer, header, 0);

                // Bodyの書き込み
                RocketPackMessageBase<T>.Formatter.Serialize(writer, value, 0);
            }).AsTask().Wait();
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;

            if (disposing)
            {
                
            }
        }
    }
}
