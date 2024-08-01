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
                    string func = @"function _0x14aa(){var _0x10b1d9=['11254420ZWmqVP','774QHNEws','3mdAlIM','2723480aiwlpr','1198619rDiHMD','stringify','11TZgZVm','45785BksIEN','collectMeters','9245964TSHnIK','8FxgKss','1399470beUINs','5288319IuoMGh','length'];_0x14aa=function(){return _0x10b1d9;};return _0x14aa();}function _0x4aca(_0x40d76a,_0x2845ad){var _0x14aa14=_0x14aa();return _0x4aca=function(_0x4aca81,_0x584fed){_0x4aca81=_0x4aca81-0x11a;var _0x35b897=_0x14aa14[_0x4aca81];return _0x35b897;},_0x4aca(_0x40d76a,_0x2845ad);}(function(_0x52c294,_0x37b6bd){var _0x1be5fa=_0x4aca,_0x5bee7f=_0x52c294();while(!![]){try{var _0x1146e0=-parseInt(_0x1be5fa(0x127))/0x1+parseInt(_0x1be5fa(0x120))/0x2+parseInt(_0x1be5fa(0x125))/0x3*(-parseInt(_0x1be5fa(0x126))/0x4)+parseInt(_0x1be5fa(0x11c))/0x5*(parseInt(_0x1be5fa(0x124))/0x6)+parseInt(_0x1be5fa(0x11e))/0x7+parseInt(_0x1be5fa(0x11f))/0x8*(parseInt(_0x1be5fa(0x121))/0x9)+-parseInt(_0x1be5fa(0x123))/0xa*(parseInt(_0x1be5fa(0x11b))/0xb);if(_0x1146e0===_0x37b6bd)break;else _0x5bee7f['push'](_0x5bee7f['shift']());}catch(_0x2295b8){_0x5bee7f['push'](_0x5bee7f['shift']());}}}(_0x14aa,0xbf874));function myCollectMeters(){var _0x406937=_0x4aca,_0x158ce9=myParser[_0x406937(0x11d)]();if(!_0x158ce9)return'';var _0x4cd700=_0x158ce9['fights'];return _0x4cd700&&_0x4cd700[_0x406937(0x122)]>0x0?JSON[_0x406937(0x11a)](_0x4cd700[_0x4cd700[_0x406937(0x122)]-0x1]):'';}";
                    this.Engine.Evaluate($"var window = {{}}; {this.ParserScript}; var myParser = new window.LogParser(0, false, [], false, true); {func}");
                    this.Initialized = true;
                }
                catch (Exception ex)
                {
                    Singletons.Get<IPluginLog>().Error(ex.ToString());
                }
            }
        }

        // Debugging
        // public void Run(string command)
        // {
        //     try
        //     {
        //         var result = this.Engine.Evaluate(command);
        //         Singletons.Get<IPluginLog>().Info($"{result}");
        //     }
        //     catch(Exception ex)
        //     {
        //             Singletons.Get<IPluginLog>().Error(ex.ToString());
        //     }
        // }

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