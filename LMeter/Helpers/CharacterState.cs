﻿using System;
using System.Collections.Generic;
using Dalamud.Game.ClientState.Conditions;
using System.Linq;
using Dalamud.Game.ClientState;

namespace LMeter.Helpers
{

    public static class CharacterState
    {
        private static readonly uint[] GoldSaucerIDs = { 144, 388, 389, 390, 391, 579, 792, 899, 941 };

        public static bool IsCharacterBusy()
        {
            Condition condition = Singletons.Get<Condition>();
            return condition[ConditionFlag.WatchingCutscene] ||
                condition[ConditionFlag.WatchingCutscene78] ||
                condition[ConditionFlag.OccupiedInCutSceneEvent] ||
                condition[ConditionFlag.CreatingCharacter] ||
                condition[ConditionFlag.BetweenAreas] ||
                condition[ConditionFlag.BetweenAreas51] ||
                condition[ConditionFlag.OccupiedSummoningBell];
        }

        public static bool IsInCombat()
        {
            Condition condition = Singletons.Get<Condition>();
            return condition[ConditionFlag.InCombat];
        }

        public static bool IsInDuty()
        {
            Condition condition = Singletons.Get<Condition>();
            return condition[ConditionFlag.BoundByDuty];
        }

        public static bool IsPerforming()
        {
            Condition condition = Singletons.Get<Condition>();
            return condition[ConditionFlag.Performing];
        }

        public static bool IsInGoldenSaucer()
        {
            return GoldSaucerIDs.Count(id => id == Singletons.Get<ClientState>().TerritoryType) > 0;
        }

        public static bool IsJob(IEnumerable<Job> jobs)
        {
            var player = Singletons.Get<ClientState>().LocalPlayer;
            if (player is null)
            {
                return false;
            }

            return jobs.Contains((Job)player.ClassJob.Id);
        }

        public static List<Job> GetJobsForJobType(JobType type)
        {
            if (type == JobType.All)
            {
                return Enum.GetValues(typeof(Job)).Cast<Job>().ToList();
            }

            if (type == JobType.Tanks)
            {
                return new List<Job>() { Job.GLA, Job.MRD, Job.PLD, Job.WAR, Job.DRK, Job.GNB };
            }

            if (type == JobType.Casters)
            {
                return new List<Job>() { Job.THM, Job.ACN, Job.BLM, Job.SMN, Job.RDM, Job.BLU };
            }

            if (type == JobType.Melee)
            {
                return new List<Job>() { Job.PGL, Job.LNC, Job.ROG, Job.MNK, Job.DRG, Job.NIN, Job.SAM };
            }

            if (type == JobType.Ranged)
            {
                return new List<Job>() { Job.ARC, Job.BRD, Job.MCH, Job.DNC };
            }

            if (type == JobType.Healers)
            {
                return new List<Job>() { Job.CNJ, Job.WHM, Job.SCH, Job.AST };
            }

            if (type == JobType.DoH)
            {
                return new List<Job>() { Job.CRP, Job.BSM, Job.ARM, Job.GSM, Job.LTW, Job.WVR, Job.ALC, Job.CUL };
            }

            if (type == JobType.DoL)
            {
                return new List<Job>() { Job.MIN, Job.BOT, Job.FSH };
            }

            if (type == JobType.DoW)
            {
                List<Job> jobList = GetJobsForJobType(JobType.Tanks);
                jobList.AddRange(GetJobsForJobType(JobType.Melee));
                jobList.AddRange(GetJobsForJobType(JobType.Ranged));
                return jobList;
            }

            if (type == JobType.DoM)
            {
                List<Job> jobList = GetJobsForJobType(JobType.Casters);
                jobList.AddRange(GetJobsForJobType(JobType.Healers));
                return jobList;
            }

            if (type == JobType.Crafters)
            {
                List<Job> jobList = GetJobsForJobType(JobType.DoH);
                jobList.AddRange(GetJobsForJobType(JobType.DoL));
                return jobList;
            }

            return new List<Job>();
        }
    }

    public enum Job
    {
        GLA = 1,
        MRD = 3,
        PLD = 19,
        WAR = 21,
        DRK = 32,
        GNB = 37,

        CNJ = 6,
        WHM = 24,
        SCH = 28,
        AST = 33,

        PGL = 2,
        LNC = 4,
        ROG = 29,
        MNK = 20,
        DRG = 22,
        NIN = 30,
        SAM = 34,

        ARC = 5,
        BRD = 23,
        MCH = 31,
        DNC = 38,

        THM = 7,
        ACN = 26,
        BLM = 25,
        SMN = 27,
        RDM = 35,
        BLU = 36,

        CRP = 8,
        BSM = 9,
        ARM = 10,
        GSM = 11,
        LTW = 12,
        WVR = 13,
        ALC = 14,
        CUL = 15,

        MIN = 16,
        BOT = 17,
        FSH = 18
    }

    public enum JobType
    {
        All,
        Custom,
        Tanks,
        Casters,
        Melee,
        Ranged,
        Healers,
        DoW,
        DoM,
        Crafters,
        DoH,
        DoL
    }
}
