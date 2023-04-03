using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Game.Gui;
using Dalamud.Game.Text;
using Dalamud.Logging;
using Dalamud.Plugin;
using LMeter.Config;
using LMeter.Helpers;
using Newtonsoft.Json;

namespace LMeter.ACT
{
    public class ACTClient : IACTClient
    {
        private ACTConfig _config;
        private ClientWebSocket _socket;
        private CancellationTokenSource _cancellationTokenSource;
        private Task? _receiveTask;
        private ACTEvent? _lastEvent;

        public const string SubscriptionMessage = """{"call":"subscribe","events":["CombatData"]}""";
        public ConnectionStatus Status { get; private set; }
        public List<ACTEvent> PastEvents { get; private set; }

        public ACTClient(ACTConfig config, DalamudPluginInterface dpi)
        {
            _config = config;
            _socket = new ClientWebSocket();
            _cancellationTokenSource = new CancellationTokenSource();
            Status = ConnectionStatus.NotConnected;
            PastEvents = new List<ACTEvent>();
        }

        public ACTEvent? GetEvent(int index = -1)
        {
            if (index >= 0 && index < PastEvents.Count)
            {
                return PastEvents[index];
            }
            
            return _lastEvent;
        }

        public void EndEncounter()
        {
            ChatGui chat = Singletons.Get<ChatGui>();
            XivChatEntry message = new XivChatEntry()
            {
                Message = "end",
                Type = XivChatType.Echo
            };

            chat.PrintChat(message);
        }

        public void Clear()
        {
            _lastEvent = null;
            PastEvents = new List<ACTEvent>();
            if (_config.ClearACT)
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

        public void RetryConnection()
        {
            Reset();
            Start();
        }

        public void Start()
        {
            if (Status != ConnectionStatus.NotConnected)
            {
                PluginLog.Error("Cannot start, ACTClient needs to be reset!");
                return;
            }

            try
            {
                _receiveTask = Task.Run(() => this.Connect(_config.ACTSocketAddress));
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

                await _socket.SendAsync(
                        Encoding.UTF8.GetBytes(SubscriptionMessage),
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
                            PluginLog.Verbose(data);
                            
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
                                                PastEvents.Add(newEvent);

                                                while (PastEvents.Count > _config.EncounterHistorySize)
                                                {
                                                    PastEvents.RemoveAt(0);
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

        public void Shutdown()
        {
            Status = ConnectionStatus.ShuttingDown;
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

                PluginLog.Information($"Closed ACT Connection");
            }

            _socket.Dispose();
            Status = ConnectionStatus.NotConnected;
        }

        public void Reset()
        {
            this.Shutdown();
            _socket = new ClientWebSocket();
            _cancellationTokenSource = new CancellationTokenSource();
            Status = ConnectionStatus.NotConnected;
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