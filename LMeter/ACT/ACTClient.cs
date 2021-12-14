using System.Threading;
using System;
using System.Net.WebSockets;
using LMeter.Helpers;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Logging;
using Newtonsoft.Json;
using Dalamud.Game.Gui;
using Dalamud.Game.Text;

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
        private ClientWebSocket Socket { get; set; }

        private CancellationTokenSource CancellationTokenSource { get; set; }

        private Task? ReceiveTask { get; set; }

        private ACTEvent? LastEvent { get; set; }

        private ConnectionStatus _status { get; set; }

        public ACTClient()
        {
            this.Socket = new ClientWebSocket();
            this.CancellationTokenSource = new CancellationTokenSource();
            this._status = ConnectionStatus.NotConnected;
        }

        public static ConnectionStatus Status => Singletons.Get<ACTClient>()._status;

        public static ACTEvent? GetLastEvent()
        {
            ACTClient client = Singletons.Get<ACTClient>();
            if (client._status != ConnectionStatus.Connected ||
                client.LastEvent is null)
            {
                return null;
            }

            return client.LastEvent;
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
            Singletons.Get<ACTClient>().LastEvent = null;
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
                this.ReceiveTask = Task.Run(() => this.Connect(host));
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
                await this.Socket.ConnectAsync(new Uri(host), this.CancellationTokenSource.Token);

                string subscribe = "{\"call\":\"subscribe\",\"events\":[\"CombatData\"]}";
                await this.Socket.SendAsync(
                        Encoding.UTF8.GetBytes(subscribe),
                        WebSocketMessageType.Text,
                        endOfMessage: true,
                        this.CancellationTokenSource.Token);
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
                            result = await this.Socket.ReceiveAsync(buffer, this.CancellationTokenSource.Token);
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
                            
                            if (!string.IsNullOrEmpty(data))
                            {
                                try
                                {
                                    ACTEvent? actEvent = JsonConvert.DeserializeObject<ACTEvent>(data);

                                    if (actEvent is not null && (CharacterState.IsInCombat() || (this.LastEvent?.IsEncounterActive() ?? false)))
                                    {
                                        actEvent.Timestamp = DateTime.UtcNow;
                                        this.LastEvent = actEvent;
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
                this.Shutdown();
            }
        }

        public void Shutdown()
        {
            this._status = ConnectionStatus.ShuttingDown;
            this.LastEvent = null;
            if (this.Socket.State == WebSocketState.Open ||
                this.Socket.State == WebSocketState.Connecting)
            {
                try
                {
                    // Close the websocket
                    this.Socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None)
                               .GetAwaiter()
                               .GetResult();
                }
                catch
                {
                    // If closing the socket failed, force it with the cancellation token.
                    this.CancellationTokenSource.Cancel();
                }

                if (this.ReceiveTask is not null)
                {
                    this.ReceiveTask.Wait();
                }

                PluginLog.Information($"Closed ACT Connection");
            }

            this.Socket.Dispose();
            this._status = ConnectionStatus.NotConnected;
        }

        public void Reset()
        {
            this.Shutdown();
            this.Socket = new ClientWebSocket();
            this.CancellationTokenSource = new CancellationTokenSource();
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