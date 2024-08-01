using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LMeter.Helpers;
using Newtonsoft.Json;

namespace LMeter.Act.DataStructures
{
    public class ActEvent : IActData<ActEvent>
    {
        [JsonIgnore]
        public static string[] TextTags { get; } = 
            typeof(ActEvent).GetMembers().Where(x => Attribute.IsDefined(x, typeof(TextTagAttribute))).Select(x => $"[{x.Name.ToLower()}]").ToArray();

        private static readonly Dictionary<string, MemberInfo> _textTagMembers = 
            typeof(ActEvent).GetMembers().Where(x => Attribute.IsDefined(x, typeof(TextTagAttribute))).ToDictionary((x) => x.Name.ToLower());

        private bool _parsedActive;
        private bool _active;

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [JsonProperty("type")]
        public string EventType { get; set; } = string.Empty;

        [JsonProperty("isActive")]
        public string IsActive { get; set; } = string.Empty;

        [JsonProperty("Encounter")]
        public Encounter? Encounter { get; set; }

        [JsonProperty("Combatant")]
        public Dictionary<string, Combatant>? Combatants { get; set; }
        
        public string GetFormattedString(string format, string numberFormat, bool emptyIfZero)
        {
            return TextTagFormatter.TextTagRegex.Replace(format, new TextTagFormatter(this, numberFormat, emptyIfZero, _textTagMembers).Evaluate);
        }

        public bool IsEncounterActive()
        {
            if (_parsedActive)
            {
                return _active;
            }

            _parsedActive = bool.TryParse(this.IsActive, out _active);
            return _active;
        }

        public bool Equals(ActEvent? actEvent)
        {
            if (actEvent is null)
            {
                return false;
            }

            if (this.Encounter is null ^ actEvent.Encounter is null ||
                this.Combatants is null ^ actEvent.Combatants is null)
            {
                return false;
            }

            if (this.Encounter is not null && actEvent.Encounter is not null &&
                this.Combatants is not null && actEvent.Combatants is not null)
            {
                return this.Encounter.DurationRaw.Equals(actEvent.Encounter.DurationRaw) &&
                       this.Encounter.Title.Equals(actEvent.Encounter.Title) &&
                       (this.Encounter.DamageTotal?.Value ?? 0f) == (actEvent.Encounter.DamageTotal?.Value ?? 0f) &&
                       this.IsEncounterActive() == actEvent.IsEncounterActive() &&
                       this.Combatants.Count == actEvent.Combatants.Count;
            }

            return true;
        }

        public void InjectFFLogsData(FFLogsMeter? meter)
        {
            string playerName = CharacterState.CharacterName;
            if (meter?.Actors is null ||
                meter?.Encounter is null ||
                this.Combatants is null ||
                this.Encounter is null ||
                string.IsNullOrEmpty(playerName))
            {
                return;
            }

            TimeSpan duration = TimeSpan.FromMilliseconds(meter.EncounterEnd - meter.EncounterStart - meter.Downtime);
            foreach (Combatant combatant in this.Combatants.Values)
            {
                string name = combatant.OriginalName;
                if (name.Equals("YOU"))
                {
                    name = playerName;
                }

                foreach (FFLogsActor actor in meter.Actors.Values)
                {
                    if (actor.Name.Equals(name))
                    {
                        combatant.FFLogsActor = actor;
                        combatant.FFLogsDuration = duration;
                    }
                }
            }
        }

        public static ActEvent GetTestData()
        {
            Dictionary<string, Combatant> mockCombatants = new()
            {
                { "1", GetCombatant("GNB", "DRK", "WAR", "PLD") },
                { "2", GetCombatant("GNB", "DRK", "WAR", "PLD") },
                { "3", GetCombatant("WHM", "AST", "SCH", "SGE") },
                { "4", GetCombatant("WHM", "AST", "SCH", "SGE") },
                { "5", GetCombatant("SAM", "DRG", "MNK", "NIN", "RPR", "VPR") },
                { "6", GetCombatant("SAM", "DRG", "MNK", "NIN", "RPR", "VPR") },
                { "7", GetCombatant("BLM", "SMN", "RDM", "PCT") },
                { "8", GetCombatant("DNC", "MCH", "BRD") },
                { "9", GetCombatant("SAM", "DRG", "MNK", "NIN", "RPR", "VPR") },
                { "10", GetCombatant("SAM", "DRG", "MNK", "NIN", "RPR", "VPR") },
                { "11", GetCombatant("BLM", "SMN", "RDM", "PCT") },
                { "12", GetCombatant("DNC", "MCH", "BRD") }
            };

            return new()
            {
                Encounter = Encounter.GetTestData(),
                Combatants = mockCombatants
            };
        }

        private static Combatant GetCombatant(params string[] jobs)
        {
            Combatant combatant = Combatant.GetTestData();
            combatant.Job = Enum.Parse<Job>(jobs[IActData<ActEvent>.Random.Next(jobs.Length)]);
            return combatant;
        }
    }
}