using Dalamud.Game.Text;
using Dalamud.Plugin.Services;
using LMeter.Config;
using LMeter.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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

    public class ACTClient : IPluginDisposable
    {
        private ACTConfig _config;
        private ClientWebSocket _socket;
        private CancellationTokenSource _cancellationTokenSource;
        private Task? _receiveTask;
        private ACTEvent? _lastEvent;
        private ConnectionStatus _status;
        private List<ACTEvent> _pastEvents;

        public static ConnectionStatus Status => Singletons.Get<ACTClient>()._status;
        public static List<ACTEvent> PastEvents => Singletons.Get<ACTClient>()._pastEvents;

        public ACTClient(ACTConfig config)
        {
            _config = config;
            _socket = new ClientWebSocket();
            _cancellationTokenSource = new CancellationTokenSource();
            _status = ConnectionStatus.NotConnected;
            _pastEvents = new List<ACTEvent>();
        }

        public static ACTEvent? GetEvent(int index = -1)
        {
            ACTClient client = Singletons.Get<ACTClient>();
            if (index >= 0 && index < client._pastEvents.Count)
            {
                return client._pastEvents[index];
            }

            return client._lastEvent;
        }

        public static void EndEncounter()
        {
            IChatGui chat = Singletons.Get<IChatGui>();
            XivChatEntry message = new XivChatEntry()
            {
                Message = "end",
                Type = XivChatType.Echo
            };

            chat.Print(message);
        }

        public void Clear()
        {
            _lastEvent = null;
            _pastEvents = new List<ACTEvent>();
            if (_config.ClearACT)
            {
                IChatGui chat = Singletons.Get<IChatGui>();
                XivChatEntry message = new XivChatEntry()
                {
                    Message = "clear",
                    Type = XivChatType.Echo
                };

                chat.Print(message);
            }
        }

        public static void RetryConnection(string address)
        {
            ACTClient client = Singletons.Get<ACTClient>();
            client.Reset();
            client.Start();
        }

        public void Start()
        {
            if (_status != ConnectionStatus.NotConnected)
            {
                Singletons.Get<IPluginLog>().Error("Cannot start, ACTClient needs to be reset!");
                return;
            }

            try
            {
                _receiveTask = Task.Run(() => this.Connect(_config.ACTSocketAddress));
            }
            catch (Exception ex)
            {
                _status = ConnectionStatus.ConnectionFailed;
                this.LogConnectionFailure(ex.ToString());
            }
        }

        private async Task Connect(string host)
        {
            try
            {
                _status = ConnectionStatus.Connecting;
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
                _status = ConnectionStatus.ConnectionFailed;
                this.LogConnectionFailure(ex.ToString());
                return;
            }

            ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[4096]);
            if (buffer.Array is null)
            {
                _status = ConnectionStatus.ConnectionFailed;
                this.LogConnectionFailure("Failed to allocate receive buffer!");
                return;
            }

            _status = ConnectionStatus.Connected;
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
                            string data = await reader.ReadToEndAsync();
                            Singletons.Get<IPluginLog>().Verbose(data);

                            if (!string.IsNullOrEmpty(data))
                            {
                                try
                                {
                                    ACTEvent? newEvent = JsonConvert.DeserializeObject<ACTEvent>(data);

                                    if (newEvent?.Encounter is not null &&
                                        newEvent?.Combatants is not null &&
                                        newEvent.Combatants.Any() &&
                                        (CharacterState.IsInCombat() || !newEvent.IsEncounterActive()))
                                    {
                                        if (!(_lastEvent is not null &&
                                            _lastEvent.IsEncounterActive() == newEvent.IsEncounterActive() &&
                                            _lastEvent.Encounter is not null &&
                                            _lastEvent.Encounter.Duration.Equals(newEvent.Encounter.Duration)))
                                        {
                                            if (!newEvent.IsEncounterActive())
                                            {
                                                _pastEvents.Add(newEvent);

                                                while (_pastEvents.Count > _config.EncounterHistorySize)
                                                {
                                                    _pastEvents.RemoveAt(0);
                                                }
                                            }

                                            newEvent.Timestamp = DateTime.UtcNow;
                                            _lastEvent = newEvent;
                                        }
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
                while (_status == ConnectionStatus.Connected);
            }
            catch
            {
                // Swallow exception in case something weird happens during shutdown
            }
            finally
            {
                if (_status != ConnectionStatus.ShuttingDown)
                {
                    this.Shutdown();
                }
            }
        }

        public void Shutdown()
        {
            _status = ConnectionStatus.ShuttingDown;
            _lastEvent = null;
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
            _status = ConnectionStatus.NotConnected;
        }

        public void Reset()
        {
            this.Shutdown();
            _socket = new ClientWebSocket();
            _cancellationTokenSource = new CancellationTokenSource();
            _status = ConnectionStatus.NotConnected;
        }

        private void LogConnectionFailure(string error)
        {
            Singletons.Get<IPluginLog>().Debug($"Failed to connect to ACT!");
            Singletons.Get<IPluginLog>().Verbose(error);
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