using System;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Services;
using LMeter.Config;
using LMeter.Helpers;
using Newtonsoft.Json.Linq;

namespace LMeter.Act
{
    public class IpcClient : LogClient
    {
        private const string IinactListeningIpcEndpoint = "IINACT.Server.Listening";
        private const string IinactSubscribeIpcEndpoint = "IINACT.CreateSubscriber";
        private const string IinactUnsubscribeIpcEndpoint = "IINACT.Unsubscribe";
        private const string LMeterSubscriptionIpcEndpoint = "LMeter.SubscriptionReceiver";
        private const string IinactProviderEditEndpoint = "IINACT.IpcProvider." + LMeterSubscriptionIpcEndpoint;
        private static readonly JObject SubscriptionMessageObject = JObject.Parse(SubscriptionMessage);

        private readonly ICallGateProvider<JObject, bool> _subscriptionReceiver;

        public IpcClient(ActConfig config) : base(config)
        {
            _subscriptionReceiver = Singletons.Get<DalamudPluginInterface>().GetIpcProvider<JObject, bool>(LMeterSubscriptionIpcEndpoint);
            _subscriptionReceiver.RegisterFunc(ReceiveIpcMessage);
        }

        public override void Start()
        {
            if (this.Status != ConnectionStatus.NotConnected)
            {
                Singletons.Get<IPluginLog>().Info("Cannot start, IpcClient needs to be reset!");
                return;
            }
            else if (!Singletons.Get<IClientState>().IsLoggedIn)
            {
                Singletons.Get<IPluginLog>().Info("Cannot start, player is not logged in.");
                return;
            }
            else if (SubscriptionMessageObject is null)
            {
                Singletons.Get<IPluginLog>().Info("Cannot start, SubscriptionMessageObject is null!");
                return;
            }

            try
            {
                this.Status = ConnectionStatus.Connecting;
                var connectSuccess = Singletons.Get<DalamudPluginInterface>().GetIpcSubscriber<bool>(IinactListeningIpcEndpoint).InvokeFunc();
                Singletons.Get<IPluginLog>().Info("Check if IINACT installed and running: " + connectSuccess);
                if (!connectSuccess)
                {
                    this.Status = ConnectionStatus.ConnectionFailed;
                    return;
                }
            }
            catch (Exception ex)
            {
                this.Status = ConnectionStatus.ConnectionFailed;
                this.LogConnectionFailure("IINACT server was not found or was not finished starting.", ex);
                return;
            }

            Singletons.Get<IPluginLog>().Info("Successfully discovered IINACT IPC endpoint");
            try
            {
                var subscribeStatus = Singletons.Get<DalamudPluginInterface>()
                    .GetIpcSubscriber<string, bool>(IinactSubscribeIpcEndpoint)
                    .InvokeFunc(LMeterSubscriptionIpcEndpoint);

                if (!subscribeStatus)
                {
                    this.Status = ConnectionStatus.ConnectionFailed;
                    return;
                }
            }
            catch (Exception ex)
            {
                this.Status = ConnectionStatus.ConnectionFailed;
                this.LogConnectionFailure("Failed to setup IINACT subscription!", ex);
                return;
            }

            try
            {
                Singletons.Get<DalamudPluginInterface>()
                    .GetIpcSubscriber<JObject, bool>(IinactProviderEditEndpoint)
                    .InvokeAction(SubscriptionMessageObject);

                this.Status = ConnectionStatus.Connected;
                Singletons.Get<IPluginLog>().Info("Successfully subscribed to combat events from IINACT IPC");
            }
            catch (Exception ex)
            {
                this.Status = ConnectionStatus.ConnectionFailed;
                this.LogConnectionFailure("Failed to finalize IINACT subscription!", ex);
            }
        }

        private bool ReceiveIpcMessage(JObject data)
        {
            try
            {
                this.ParseLogData(data);
                return true;
            }
            catch (Exception ex)
            {
                this.LogConnectionFailure(ex.ToString());
            }
            
            return false;
        }

        public override void Reset()
        {
            this.Shutdown();
            this.Start();
        }

        public override void Shutdown()
        {
            this.Status = ConnectionStatus.ShuttingDown;

            try
            {
                var success = Singletons.Get<DalamudPluginInterface>()
                    .GetIpcSubscriber<string, bool>(IinactUnsubscribeIpcEndpoint)
                    .InvokeFunc(LMeterSubscriptionIpcEndpoint);

                Singletons.Get<IPluginLog>().Info(
                    success
                        ? "Successfully unsubscribed from IINACT IPC"
                        : "Failed to unsubscribe from IINACT IPC"
                );
            }
            catch (Exception)
            {
                // don't throw when closing
            }

            this.Status = ConnectionStatus.NotConnected;
        }
    }
}