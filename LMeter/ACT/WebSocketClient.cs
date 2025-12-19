using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using LMeter.Config;
using LMeter.Helpers;

namespace LMeter.Act
{
    public class WebSocketClient(ActConfig config) : LogClient(config)
    {
        private ClientWebSocket _socket = new ClientWebSocket();
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private Task? _receiveTask;

        public override void Start()
        {
            if (this.Status != ConnectionStatus.NotConnected)
            {
                Singletons.Get<IPluginLog>().Error("Cannot start, ACT client needs to be reset!");
                return;
            }

            try
            {
                _receiveTask = Task.Run(() => this.Connect(Config.ActSocketAddress));
            }
            catch (Exception ex)
            {
                this.Status = ConnectionStatus.ConnectionFailed;
                if (Config.LogConnectionErrors)
                {
                    LogConnectionFailure(ex.ToString());
                }
            }
        }

        private async Task Connect(string host)
        {
            try
            {
                this.Status = ConnectionStatus.Connecting;
                await _socket.ConnectAsync(new Uri(host), _cancellationTokenSource.Token);

                await _socket.SendAsync(
                    Encoding.UTF8.GetBytes(SubscriptionMessage),
                    WebSocketMessageType.Text,
                    endOfMessage: true,
                    _cancellationTokenSource.Token
                );
            }
            catch (Exception ex)
            {
                this.Status = ConnectionStatus.ConnectionFailed;
                if (Config.LogConnectionErrors)
                {
                    LogConnectionFailure(ex.ToString());
                }

                return;
            }

            ArraySegment<byte> buffer = new(new byte[4096]);
            if (buffer.Array is null)
            {
                this.Status = ConnectionStatus.ConnectionFailed;
                LogConnectionFailure("Failed to allocate receive buffer!");
                return;
            }

            this.Status = ConnectionStatus.Connected;
            Singletons.Get<IPluginLog>().Information("Successfully Established ACT Connection");
            try
            {
                do
                {
                    WebSocketReceiveResult result;
                    using (MemoryStream ms = new())
                    {
                        do
                        {
                            result = await _socket.ReceiveAsync(buffer, _cancellationTokenSource.Token);
                            ms.Write(buffer.Array, buffer.Offset, result.Count);
                        } while (!result.EndOfMessage);

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            break;
                        }

                        ms.Seek(0, SeekOrigin.Begin);
                        using (StreamReader reader = new(ms, Encoding.UTF8))
                        {
                            try
                            {
                                string data = await reader.ReadToEndAsync();
                                this.ParseLogData(data);
                            }
                            catch (Exception ex)
                            {
                                LogConnectionFailure(ex.ToString());
                            }
                        }
                    }
                } while (this.Status == ConnectionStatus.Connected);
            }
            catch
            {
                // Swallow exception in case something weird happens during shutdown
            }
            finally
            {
                if (this.Status != ConnectionStatus.ShuttingDown)
                {
                    this.Shutdown();
                }
            }
        }

        public override void Shutdown()
        {
            this.Status = ConnectionStatus.ShuttingDown;
            if (_socket.State == WebSocketState.Open || _socket.State == WebSocketState.Connecting)
            {
                try
                {
                    // Close the websocket
                    _socket
                        .CloseOutputAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None)
                        .GetAwaiter()
                        .GetResult();
                }
                catch
                {
                    // If closing the socket failed, force it with the cancellation token.
                    _cancellationTokenSource.Cancel();
                }

                if (_receiveTask is not null)
                {
                    _receiveTask.Wait();
                }

                Singletons.Get<IPluginLog>().Information($"Closed ACT Connection");
            }

            _socket.Dispose();
            this.Status = ConnectionStatus.NotConnected;
        }

        public override void Reset()
        {
            this.Shutdown();
            _socket = new ClientWebSocket();
            _cancellationTokenSource = new CancellationTokenSource();
            this.Start();
        }
    }
}
