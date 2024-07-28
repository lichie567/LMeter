using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Character;

namespace LMeter.Helpers
{
    public static class CharacterState
    {
        private static readonly uint[] _goldSaucerIds = [144, 388, 389, 390, 391, 579, 792, 899, 941];
        private static readonly ushort[] _houseIds = [
            // Small, Medium, Large, Chamber, Apartment
            282, 283, 284, 384, 608, // Mist
            342, 343, 344, 385, 609, // Lavender Beds
            345, 346, 347, 386, 610, // Goblet
            649, 650, 651, 652, 655, // Shirogane
            980, 981, 982, 983, 999, // Empyreum 
        ];

        public static string CharacterName { get; private set; } = string.Empty;

        public static void UpdateCurrentCharacter()
        {
            string? playerName = Singletons.Get<IClientState>().LocalPlayer?.Name.ToString();
            if (!string.IsNullOrEmpty(playerName))
            {
                CharacterName = playerName;
            }
        }

        public static bool IsCharacterBusy()
        {
            ICondition condition = Singletons.Get<ICondition>();
            return condition[ConditionFlag.WatchingCutscene] ||
                condition[ConditionFlag.WatchingCutscene78] ||
                condition[ConditionFlag.OccupiedInCutSceneEvent] ||
                condition[ConditionFlag.CreatingCharacter] ||
                condition[ConditionFlag.BetweenAreas] ||
                condition[ConditionFlag.BetweenAreas51] ||
                condition[ConditionFlag.OccupiedSummoningBell];
        }

        public static bool IsInCombat() => Singletons.Get<ICondition>()[ConditionFlag.InCombat];
        public static bool IsInDuty() => Singletons.Get<ICondition>()[ConditionFlag.BoundByDuty];
        public static bool IsPerforming() => Singletons.Get<ICondition>()[ConditionFlag.Performing];
        public static bool IsEditingHouse() => Singletons.Get<ICondition>()[ConditionFlag.UsingHousingFunctions];
        public static bool IsInDeepDungeon() => Singletons.Get<ICondition>()[ConditionFlag.InDeepDungeon];
        public static bool InZone(ZoneType zone) => zone switch
        {
            ZoneType.GoldSaucer => _goldSaucerIds.Any(id => id == Singletons.Get<IClientState>().TerritoryType),
            ZoneType.PlayerHouse => _houseIds.Any(id => id == Singletons.Get<IClientState>().TerritoryType),
            _ => false
        };

        public static Job GetCharacterJob()
        {
            IPlayerCharacter? player = Singletons.Get<IClientState>().LocalPlayer;
            if (player is null)
            {
                return Job.UKN;
            }

            unsafe
            {
                return (Job)((Character*)player.Address)->CharacterData.ClassJob;
            }
        }

        public static bool IsJobType(Job job, JobType type, IEnumerable<Job>? jobList = null) => type switch
        {
            JobType.All => true,
            JobType.Tanks => job is Job.GLA or Job.MRD or Job.PLD or Job.WAR or Job.DRK or Job.GNB,
            JobType.Casters => job is Job.THM or Job.ACN or Job.BLM or Job.SMN or Job.RDM or Job.PCT or Job.BLU,
            JobType.Melee => job is Job.PGL or Job.LNC or Job.ROG or Job.MNK or Job.DRG or Job.NIN or Job.SAM or Job.RPR or Job.VPR,
            JobType.Ranged => job is Job.ARC or Job.BRD or Job.MCH or Job.DNC,
            JobType.Healers => job is Job.CNJ or Job.WHM or Job.SCH or Job.AST or Job.SGE,
            JobType.DoH => job is Job.CRP or Job.BSM or Job.ARM or Job.GSM or Job.LTW or Job.WVR or Job.ALC or Job.CUL,
            JobType.DoL => job is Job.MIN or Job.BOT or Job.FSH,
            JobType.Combat => IsJobType(job, JobType.DoW) || IsJobType(job, JobType.DoM),
            JobType.DoW => IsJobType(job, JobType.Tanks) || IsJobType(job, JobType.Melee) || IsJobType(job, JobType.Ranged),
            JobType.DoM => IsJobType(job, JobType.Casters) || IsJobType(job, JobType.Healers),
            JobType.Crafters => IsJobType(job, JobType.DoH) || IsJobType(job, JobType.DoL),
            JobType.Custom => jobList is not null && jobList.Contains(job),
            _ => false
        };
    }
}
