using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using LMeter.Act.DataStructures;
using LMeter.Config;
using LMeter.Helpers;
using Newtonsoft.Json;

namespace LMeter.Act
{
    public class WebSocketClient : LogClient
    {
        private ClientWebSocket _socket;
        private CancellationTokenSource _cancellationTokenSource;
        private Task? _receiveTask;

        public WebSocketClient(ActConfig config) : base(config)
        {
            _socket = new ClientWebSocket();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public override void Start()
        {
            if (Status != ConnectionStatus.NotConnected)
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
                Status = ConnectionStatus.ConnectionFailed;
                this.LogConnectionFailure(ex.ToString());
            }
        }

        private async Task Connect(string host)
        {
            try
            {
                Status = ConnectionStatus.Connecting;
                await _socket.ConnectAsync(new Uri(host), _cancellationTokenSource.Token);

                string subscribe = "{\"call\":\"subscribe\",\"events\":[\"CombatData\"]}";
                await _socket.SendAsync(
                        Encoding.UTF8.GetBytes(subscribe),
                        WebSocketMessageType.Text,
                        endOfMessage: true,
                        _cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                Status = ConnectionStatus.ConnectionFailed;
                this.LogConnectionFailure(ex.ToString());
                return;
            }

            ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[4096]);
            if (buffer.Array is null)
            {
                Status = ConnectionStatus.ConnectionFailed;
                this.LogConnectionFailure("Failed to allocate receive buffer!");
                return;
            }

            Status = ConnectionStatus.Connected;
            Singletons.Get<IPluginLog>().Information("Successfully Established ACT Connection");
            try
            {
                do
                {
                    WebSocketReceiveResult result;
                    using (MemoryStream ms = new MemoryStream())
                    {
                        do
                        {
                            result = await _socket.ReceiveAsync(buffer, _cancellationTokenSource.Token);
                            ms.Write(buffer.Array, buffer.Offset, result.Count);
                        }
                        while (!result.EndOfMessage);

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            break;
                        }

                        ms.Seek(0, SeekOrigin.Begin);
                        using (StreamReader reader = new StreamReader(ms, Encoding.UTF8))
                        {
                            try
                            {
                                string data = await reader.ReadToEndAsync();
                                ActEvent? newEvent = JsonConvert.DeserializeObject<ActEvent>(data);
                                this.ParseLogData(newEvent);
                            }
                            catch (Exception ex)
                            {
                                this.LogConnectionFailure(ex.ToString());
                            }
                        }
                    }
                }
                while (Status == ConnectionStatus.Connected);
            }
            catch
            {
                // Swallow exception in case something weird happens during shutdown
            }
            finally
            {
                if (Status != ConnectionStatus.ShuttingDown)
                {
                    this.Shutdown();
                }
            }
        }

        public override void Shutdown()
        {
            Status = ConnectionStatus.ShuttingDown;
            if (_socket.State == WebSocketState.Open ||
                _socket.State == WebSocketState.Connecting)
            {
                try
                {
                    // Close the websocket
                    _socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None)
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
            Status = ConnectionStatus.NotConnected;
        }

        public override void Reset()
        {
            this.Shutdown();
            _socket = new ClientWebSocket();
            _cancellationTokenSource = new CancellationTokenSource();
            Status = ConnectionStatus.NotConnected;
        }
    }
}