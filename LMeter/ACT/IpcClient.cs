using System;
using System.Text.Json.Nodes;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Services;
using LMeter.Config;
using LMeter.Helpers;

namespace LMeter.Act
{
    public class IpcClient : LogClient
    {
        private const string IinactListeningIpcEndpoint = "IINACT.Server.Listening";
        private const string IinactSubscribeIpcEndpoint = "IINACT.CreateSubscriber";
        private const string IinactUnsubscribeIpcEndpoint = "IINACT.Unsubscribe";

        private const string LMeterSubscriptionIpcEndpoint = "LMeter.SubscriptionReceiver";
        private const string IinactProviderEditEndpoint = "IINACT.IpcProvider." + LMeterSubscriptionIpcEndpoint;
        public const string SubscriptionMessage = """{"call":"subscribe","events":["CombatData"]}""";
        private static readonly JsonObject? SubscriptionMessageObject = JsonNode.Parse(SubscriptionMessage)?.AsObject();

        private readonly ICallGateProvider<JsonObject, bool> subscriptionReceiver;

        public IpcClient(ActConfig config) : base(config)
        {
            subscriptionReceiver = Singletons.Get<DalamudPluginInterface>().GetIpcProvider<JsonObject, bool>(LMeterSubscriptionIpcEndpoint);
            subscriptionReceiver.RegisterFunc(ReceiveIpcMessage);

        }

        public override void Start()
        {
            if (this.Status != ConnectionStatus.NotConnected)
            {
                Singletons.Get<IPluginLog>().Info("Cannot start, IINACTClient needs to be reset!");
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
                Singletons.Get<IPluginLog>().Verbose("Check if IINACT installed and running: " + connectSuccess);
                if (!connectSuccess)
                {
                    this.Status = ConnectionStatus.ConnectionFailed;
                    return;
                }
            }
            catch (Exception ex)
            {
                this.Status = ConnectionStatus.ConnectionFailed;
                Singletons.Get<IPluginLog>().Info("IINACT server was not found or was not finished starting.");
                Singletons.Get<IPluginLog>().Debug(ex.ToString());
                return;
            }

            Singletons.Get<IPluginLog>().Info("Successfully discovered IINACT IPC endpoint");

            try
            {
                var subscribeSuccess = Singletons.Get<DalamudPluginInterface>()
                    .GetIpcSubscriber<string, bool>(IinactSubscribeIpcEndpoint)
                    .InvokeFunc(LMeterSubscriptionIpcEndpoint);

                Singletons.Get<IPluginLog>().Verbose("Setup default empty IINACT subscription successfully: " + subscribeSuccess);
                if (!subscribeSuccess)
                {
                    this.Status = ConnectionStatus.ConnectionFailed;
                    return;
                }
            }
            catch (Exception ex)
            {
                this.Status = ConnectionStatus.ConnectionFailed;
                Singletons.Get<IPluginLog>().Info("Failed to setup IINACT subscription!");
                Singletons.Get<IPluginLog>().Debug(ex.ToString());
                return;
            }

            try
            {
                // no way to check this, hoping blindly that it always works ¯\_(ツ)_/¯
                Singletons.Get<IPluginLog>().Verbose($"""Updating subscription using endpoint: `{IinactProviderEditEndpoint}`""");
                Singletons.Get<DalamudPluginInterface>()
                    .GetIpcSubscriber<JsonObject, bool>(IinactProviderEditEndpoint)
                    .InvokeAction(SubscriptionMessageObject);
                Singletons.Get<IPluginLog>().Verbose($"""Subscription update message sent""");
                this.Status = ConnectionStatus.Connected;
                Singletons.Get<IPluginLog>().Info("Successfully subscribed to combat events from IINACT IPC");
            }
            catch (Exception ex)
            {
                this.Status = ConnectionStatus.ConnectionFailed;
                Singletons.Get<IPluginLog>().Info("Failed to finalize IINACT subscription!");
                Singletons.Get<IPluginLog>().Debug(ex.ToString());
            }
        }

        private bool ReceiveIpcMessage(JsonObject json)
        {
            try
            {
                string data = json.ToJsonString();
                this.ParseLogData(data);
                return true;
            }
            catch (Exception ex)
            {
                Singletons.Get<IPluginLog>().Debug(ex.ToString());
                return false;
            }
        }

        public override void Reset()
        {
            throw new NotImplementedException();
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