using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Game.Gui;
using Dalamud.Game.Text;
using Dalamud.Logging;
using LMeter.Helpers;
using Newtonsoft.Json;

namespace LMeter.ACT
{
    public enum ConnectionStatus
    {
        NotConnected,
        Connected,
        ShuttingDown,
        Connecting,
        ConnectionFailed
    }

    public class ACTClient : ILMeterDisposable
    {
        private ClientWebSocket _socket;
        private CancellationTokenSource _cancellationTokenSource;
        private Task? _receiveTask;
        private ACTEvent? _lastEvent;
        private ConnectionStatus _status;

        public ACTClient()
        {
            this._socket = new ClientWebSocket();
            this._cancellationTokenSource = new CancellationTokenSource();
            this._status = ConnectionStatus.NotConnected;
        }

        public static ConnectionStatus Status => Singletons.Get<ACTClient>()._status;

        public static ACTEvent? GetLastEvent()
        {
            ACTClient client = Singletons.Get<ACTClient>();
            if (client._status != ConnectionStatus.Connected ||
                client._lastEvent is null)
            {
                return null;
            }

            return client._lastEvent;
        }

        public static void EndEncounter()
        {
            ChatGui chat = Singletons.Get<ChatGui>();
            XivChatEntry message = new XivChatEntry()
            {
                Message = "end",
                Type = XivChatType.Echo
            };

            chat.PrintChat(message);
        }

        public static void Clear(bool clearAct)
        {
            Singletons.Get<ACTClient>()._lastEvent = null;
            if (clearAct)
            {
                ChatGui chat = Singletons.Get<ChatGui>();
                XivChatEntry message = new XivChatEntry()
                {
                    Message = "clear",
                    Type = XivChatType.Echo
                };

                chat.PrintChat(message);
            }
        }

        public static void RetryConnection(string address)
        {
            ACTClient client = Singletons.Get<ACTClient>();
            client.Reset();
            client.Start(address);
        }

        public void Start(string host)
        {
            if (this._status != ConnectionStatus.NotConnected)
            {
                PluginLog.Error("Cannot start, ACTClient needs to be reset!");
                return;
            }

            try
            {
                this._receiveTask = Task.Run(() => this.Connect(host));
            }
            catch (Exception ex)
            {
                this._status = ConnectionStatus.ConnectionFailed;
                this.LogConnectionFailure(ex.ToString());
            }
        }

        private async Task Connect(string host)
        {
            try
            {
                this._status = ConnectionStatus.Connecting;
                await this._socket.ConnectAsync(new Uri(host), this._cancellationTokenSource.Token);

                string subscribe = "{\"call\":\"subscribe\",\"events\":[\"CombatData\"]}";
                await this._socket.SendAsync(
                        Encoding.UTF8.GetBytes(subscribe),
                        WebSocketMessageType.Text,
                        endOfMessage: true,
                        this._cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                this._status = ConnectionStatus.ConnectionFailed;
                this.LogConnectionFailure(ex.ToString());
                return;
            }

            ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[4096]);
            if (buffer.Array is null)
            {
                this._status = ConnectionStatus.ConnectionFailed;
                this.LogConnectionFailure("Failed to allocate receive buffer!");
                return;
            }

            this._status = ConnectionStatus.Connected;
            PluginLog.Information("Successfully Established ACT Connection");
            try
            {
                do
                {
                    WebSocketReceiveResult result;
                    using (MemoryStream ms = new MemoryStream())
                    {
                        do
                        {
                            result = await this._socket.ReceiveAsync(buffer, this._cancellationTokenSource.Token);
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
                            string data = await reader.ReadToEndAsync();
                            
                            PluginLog.Verbose(data);
                            if (!string.IsNullOrEmpty(data))
                            {
                                try
                                {
                                    ACTEvent? actEvent = JsonConvert.DeserializeObject<ACTEvent>(data);

                                    if (actEvent?.Combatants is not null &&
                                        actEvent.Combatants.Count > 0 &&
                                        (CharacterState.IsInCombat() || !actEvent.IsEncounterActive()))
                                    {
                                        actEvent.Timestamp = DateTime.UtcNow;
                                        this._lastEvent = actEvent;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    this.LogConnectionFailure(ex.ToString());
                                }
                            }
                        }
                    }
                }
                while (this._status == ConnectionStatus.Connected);
            }
            catch
            {
                // Swallow exception in case something weird happens during shutdown
            }
            finally
            {
                if (this._status != ConnectionStatus.ShuttingDown)
                {
                    this.Shutdown();
                }
            }
        }

        public void Shutdown()
        {
            this._status = ConnectionStatus.ShuttingDown;
            this._lastEvent = null;
            if (this._socket.State == WebSocketState.Open ||
                this._socket.State == WebSocketState.Connecting)
            {
                try
                {
                    // Close the websocket
                    this._socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None)
                               .GetAwaiter()
                               .GetResult();
                }
                catch
                {
                    // If closing the socket failed, force it with the cancellation token.
                    this._cancellationTokenSource.Cancel();
                }

                if (this._receiveTask is not null)
                {
                    this._receiveTask.Wait();
                }

                PluginLog.Information($"Closed ACT Connection");
            }

            this._socket.Dispose();
            this._status = ConnectionStatus.NotConnected;
        }

        public void Reset()
        {
            this.Shutdown();
            this._socket = new ClientWebSocket();
            this._cancellationTokenSource = new CancellationTokenSource();
            this._status = ConnectionStatus.NotConnected;
        }

        private void LogConnectionFailure(string error)
        {
            PluginLog.Debug($"Failed to connect to ACT!");
            PluginLog.Verbose(error);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.Shutdown();
            }
        }
    }
}