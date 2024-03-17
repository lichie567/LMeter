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
using LMeter.Act.DataStructures;

namespace LMeter.Act
{

    public class ActClient : IPluginDisposable
    {
        private ActConfig _config;
        private ClientWebSocket _socket;
        private ConnectionStatus _status;
        private CancellationTokenSource _cancellationTokenSource;
        private Task? _receiveTask;
        private ActEvent? _lastEvent;
        private ActEvent? _currentEvent;
        private List<ActEvent> _pastEvents;

        public static ConnectionStatus Status => Singletons.Get<ActClient>()._status;
        public static List<ActEvent> PastEvents => Singletons.Get<ActClient>()._pastEvents;

        public ActClient(ActConfig config)
        {
            _config = config;
            _socket = new ClientWebSocket();
            _cancellationTokenSource = new CancellationTokenSource();
            _status = ConnectionStatus.NotConnected;
            _pastEvents = new List<ActEvent>();
        }

        public static ActEvent? GetEvent(int index = -1)
        {
            ActClient client = Singletons.Get<ActClient>();
            if (index >= 0 && index < client._pastEvents.Count)
            {
                return client._pastEvents[index];
            }

            return client._currentEvent;
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
            _currentEvent = null;
            _pastEvents = new List<ActEvent>();
            if (_config.ClearAct)
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
            ActClient client = Singletons.Get<ActClient>();
            client.Reset();
            client.Start();
        }

        public void Start()
        {
            if (_status != ConnectionStatus.NotConnected)
            {
                Singletons.Get<IPluginLog>().Error("Cannot start, ACT client needs to be reset!");
                return;
            }

            try
            {
                _receiveTask = Task.Run(() => this.Connect(_config.ActSocketAddress));
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
                                    ActEvent? newEvent = JsonConvert.DeserializeObject<ActEvent>(data);
                                    if (newEvent is not null)
                                    {
                                        newEvent.Timestamp = DateTime.UtcNow;
                                        newEvent.Data = data;

                                        if (newEvent?.Encounter is not null &&
                                            newEvent?.Combatants is not null &&
                                            newEvent.Combatants.Any() &&
                                            !newEvent.Equals(_lastEvent))
                                        {
                                            if (!newEvent.IsEncounterActive())
                                            {
                                                _pastEvents.Add(newEvent);
                                                while (_pastEvents.Count > _config.EncounterHistorySize)
                                                {
                                                    _pastEvents.RemoveAt(0);
                                                }
                                            }

                                            _lastEvent = newEvent;
                                            _currentEvent = newEvent;
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