using System.Threading;
using System;
using System.Net.WebSockets;
using LMeter.Helpers;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Logging;
using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;

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

        public ConnectionStatus Status { get; private set; }

        private ACTEvent? LastEvent { get; set; }

        public ACTClient()
        {
            this.Socket = new ClientWebSocket();
            this.CancellationTokenSource = new CancellationTokenSource();
            this.Status = ConnectionStatus.NotConnected;
        }

        public static bool GetLastEvent([MaybeNullWhen(false)] out ACTEvent actEvent)
        {
            actEvent = null!;

            ACTClient client = Singletons.Get<ACTClient>();
            if (client.Status != ConnectionStatus.Connected ||
                client.LastEvent is null)
            {
                return false;
            }

            actEvent = client.LastEvent;
            return true;
        }

        public void Start(string host)
        {
            if (this.Status != ConnectionStatus.NotConnected)
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
                this.Status = ConnectionStatus.ConnectionFailed;
                PluginLog.Error($"{ex.ToString()}");
            }
        }

        private async Task Connect(string host)
        {
            try
            {
                this.Status = ConnectionStatus.Connecting;
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
                this.Status = ConnectionStatus.ConnectionFailed;
                PluginLog.Error($"Failed to connect to ACT!\n{ex.ToString()}");
                return;
            }

            ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[4096]);
            if (buffer.Array is null)
            {
                this.Status = ConnectionStatus.ConnectionFailed;
                PluginLog.Error($"Failed to connect to ACT!\nFailed to allocate receive buffer!");
                return;
            }

            this.Status = ConnectionStatus.Connected;
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

                                    if (actEvent is not null)
                                    {
                                        this.LastEvent = actEvent;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    PluginLog.Error(ex.ToString());
                                }
                            }
                        }
                    }
                }
                while (this.Status == ConnectionStatus.Connected);
            }
            catch
            {
                // Swallow exception in case something weird happens during shutdown
            }
        }

        public void Shutdown()
        {
            this.Status = ConnectionStatus.ShuttingDown;
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
        }

        public void Reset()
        {
            this.Shutdown();
            this.Socket = new ClientWebSocket();
            this.CancellationTokenSource = new CancellationTokenSource();
            this.Status = ConnectionStatus.NotConnected;
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