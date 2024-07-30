using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using Microsoft.ClearScript.V8;
using Dalamud.Plugin.Services;
using LMeter.Helpers;
using Newtonsoft.Json;

namespace LMeter.Act
{
    public partial class FFLogsClient : IPluginDisposable
    {
        [GeneratedRegex(@"https://assets.rpglogs.com/js/log-parsers/parser-ff\.[a-f0-9]+\.js", RegexOptions.Compiled)]
        private static partial Regex GeneratedRegex();
        private static Regex ParserUrlRegex { get; } = GeneratedRegex();

        private const string FFLOGS_URL = "https://www.fflogs.com/desktop-client/parser";
        private static readonly string LMETER_USERAGENT = $"LMeter/{Plugin.Version} (+https://github.com/lichie567/LMeter)";

        private V8ScriptEngine Engine { get; set; }
        private string ParserScript { get; init; }

        public bool Initialized { get; private set; }

        public FFLogsClient()
        {
            this.Initialized = false;
            this.ParserScript = string.Empty;
            this.Engine = new V8ScriptEngine();

            try
            {
                HttpClient httpClient = new();
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(LMETER_USERAGENT);
                HttpResponseMessage response = httpClient.GetAsync(FFLOGS_URL).GetAwaiter().GetResult();
                string result = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                Match match = ParserUrlRegex.Match(result);
                if (match.Success)
                {
                    string parser_url = match.Groups[0].ToString();
                    response = httpClient.GetAsync(parser_url).GetAwaiter().GetResult();
                    this.ParserScript = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                }
                
                this.InitializeEngine();
            }
            catch (Exception ex)
            {
                Singletons.Get<IPluginLog>().Error(ex.ToString());
            }
        }

        private void InitializeEngine()
        {
            if (!this.Initialized && !string.IsNullOrEmpty(this.ParserScript))
            {
                try
                {
                    string func = @"function myCollectMeters() {var meters = myParser.collectMeters(); if(meters && meters.length > 0) {return JSON.stringify(meters[meters.length-1])} else {return """"}}";
                    this.Engine.Evaluate($"var window = {{}}; {this.ParserScript}; var myParser = new window.LogParser(0, false, [], false, true); {func}");
                    this.Initialized = true;
                }
                catch (Exception ex)
                {
                    Singletons.Get<IPluginLog>().Error(ex.ToString());
                }
            }
        }

        public void ParseLine(string logLine)
        {
            if (this.Initialized)
            {
                try
                {
                    this.Engine.Script.myParser.parseLine(logLine);
                }
                catch (Exception ex)
                {
                    Singletons.Get<IPluginLog>().Error(ex.ToString());
                }
            }
        }

        public FFLogsMeter? CollectMeters()
        {
            if (this.Initialized)
            {
                try
                {
                    string? result = this.Engine.Script.myCollectMeters() as string;
                    if (!string.IsNullOrEmpty(result))
                    {
                        return JsonConvert.DeserializeObject<FFLogsMeter>(result);
                    }
                }
                catch (Exception ex)
                {
                    Singletons.Get<IPluginLog>().Error(ex.ToString());
                }
            }

            return null;
        }

        public void Reset()
        {
            this.Initialized = false;
            this.Engine.Dispose();
            this.Engine = new V8ScriptEngine();
            this.InitializeEngine();
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
                this.Initialized = false;
                this.Engine.Dispose();
            }
        }
    }

    public class FFLogsMeter
    {
        [JsonProperty("startTime")]
        public long EncounterStart { get; set; }

        [JsonProperty("endTime")]
        public long EncounterEnd { get; set; }

        [JsonProperty("downtime")]
        public int Downtime { get; set; }

        [JsonProperty("encounter")]
        public FFLogsEncounter? Encounter { get; set; }

        [JsonProperty("state")]
        public string State { get; set; } = string.Empty;

        [JsonProperty("friendlyDamage")]
        private FFLogsActors? _actors = null;

        [JsonIgnore]
        public Dictionary<string, FFLogsActor>? Actors => _actors?.Actors;

        [JsonIgnore]
        public bool IsEncounterActive => this.State.Equals("inprogress");
    }

    public class FFLogsEncounter
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; } = string.Empty;

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;
    }

    public class FFLogsActors
    {
        [JsonProperty("actors")]
        public Dictionary<string, FFLogsActor>? Actors { get; set; }
    }

    public class FFLogsActor
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("amount")]
        public float Amount { get; set; }

        [JsonProperty("amountGiven")]
        public float AmountGiven { get; set; }

        [JsonProperty("amountTaken")]
        public float AmountTaken { get; set; }

        [JsonProperty("singleTargetAmountTaken")]
        public float SingleTargetAmountTaken { get; set; }

        [JsonProperty("over")]
        public float Overkill { get; set; }

        [JsonProperty("abilities")]
        public Dictionary<string, FFLogsActor>? Abilities { get; set; }

        public override string ToString()
        {
            return $"name: {this.Name}, amount: {this.Amount}, amountGiven: {this.AmountGiven}, amountTaken: {this.AmountTaken}, singleTargetAmountTaken: {this.SingleTargetAmountTaken}";
        }
    }

    public class FFLogsAbility
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("amount")]
        public float Amount { get; set; }

        [JsonProperty("amountGiven")]
        public float AmountGiven { get; set; }

        [JsonProperty("amountTaken")]
        public float AmountTaken { get; set; }

        [JsonProperty("singleTargetAmountTaken")]
        public float SingleTargetAmountTaken { get; set; }

        public override string ToString()
        {
            return $"name: {this.Name}, amount: {this.Amount}, amountGiven: {this.AmountGiven}, amountTaken: {this.AmountTaken}, singleTargetAmountTaken: {this.SingleTargetAmountTaken}";
        }
    }
}