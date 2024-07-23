using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using Microsoft.ClearScript.V8;
using Dalamud.Plugin.Services;
using LMeter.Helpers;

namespace LMeter.Act
{
    public partial class FFLogsClient : IPluginDisposable
    {
        [GeneratedRegex(@"https://assets.rpglogs.com/js/log-parsers/parser-ff\.[a-f0-9]+\.js", RegexOptions.Compiled)]
        private static partial Regex GeneratedRegex();
        private static Regex ParserUrlRegex { get; } = GeneratedRegex();

        private const string FFLOGS_URL = "https://www.fflogs.com/desktop-client/parser";
        private static readonly string LMETER_USERAGENT = $"LMeter/{Plugin.Version} (+https://github.com/lichie567/LMeter)";

        private V8ScriptEngine Engine { get; init; }

        public FFLogsClient()
        {
            this.Engine = new V8ScriptEngine();

            try
            {
                HttpClient httpClient = new();
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(LMETER_USERAGENT);
                var response = httpClient.GetAsync(FFLOGS_URL).GetAwaiter().GetResult();
                string result = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                var match = ParserUrlRegex.Match(result);
                if (match.Success)
                {
                    string parser_url = match.Groups[0].ToString();
                    response = httpClient.GetAsync(parser_url).GetAwaiter().GetResult();
                    string script = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    Singletons.Get<IPluginLog>().Info($"parser_url: {parser_url}");
                    Singletons.Get<IPluginLog>().Info($"script: {script}");

                    if (!string.IsNullOrEmpty(script))
                    {
                        var output = this.Engine.Evaluate($"var window = {{}}; {script}; var myParser = new window.LogParser(0, false, [], false, true);");
                        Singletons.Get<IPluginLog>().Info($"output: {output}");
                    }
                }
            }
            catch (Exception ex)
            {
                Singletons.Get<IPluginLog>().Error($"exception: {ex}");
            }
        }

        public void ParseLine(string logLine)
        {
            var result = this.Engine.Script.myParser.parseLine(logLine);
            Singletons.Get<IPluginLog>().Info($"collectMeters: {result}");
        }

        public string CollectMeters()
        {
            var result = this.Engine.Script.myParser.collectMeters();
            Singletons.Get<IPluginLog>().Info($"collectMeters: {result}");
            return $"{result}";
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
                this.Engine.Dispose();
            }
        }
    }
}