using System;
using LMeter.Helpers;

namespace LMeter.ACT
{
    public class LazyString<T>
    {
        private string _value = string.Empty;
        private Func<T, string> _generator;
        private Func<T> _getInput;

        public bool WasGenerated { get; private set; }
        
        public string Value
        {
            get
            {
                if (this.WasGenerated)
                {
                    return this._value;
                }
                
                this._value = this._generator.Invoke(this._getInput.Invoke());
                this.WasGenerated = true;
                return this._value;
            }
        }

        public LazyString(Func<T> getInput, Func<T, string> generator)
        {
            this._getInput = getInput;
            this._generator = generator;
        }

        public override string? ToString()
        {
            return this.Value;
        }
    }

    public static class LazyStringConverters
    {
        public static string FirstName(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            string[] splits = input.Split(" ");
            if (splits.Length < 2)
            {
                return input;
            }

            return splits[0];
        }
        
        public static string LastName(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            string[] splits = input.Split(" ");
            if (splits.Length < 2)
            {
                return string.Empty;
            }

            return splits[1];
        }

        public static string MaxHitName(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            string[] splits = input.Split('-');
            if (splits.Length < 2)
            {
                return input;
            }

            return splits[0];
        }

        public static string MaxHitValue(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            string[] splits = input.Split('-');
            if (splits.Length < 2)
            {
                return input;
            }

            return splits[1];
        }

        public static string JobName(Job input) => input switch
        {
            Job.GLA => "Gladiator",
            Job.MRD => "Marauder",
            Job.PLD => "Paladin",
            Job.WAR => "Warrior",
            Job.DRK => "Dark Knight",
            Job.GNB => "Gunbreaker",

            Job.CNJ => "Conjurer",
            Job.WHM => "White Mage",
            Job.SCH => "Scholar",
            Job.AST => "Astrologian",
            Job.SGE => "Sage",

            Job.PGL => "Pugilist",
            Job.LNC => "Lancer",
            Job.ROG => "Rogue",
            Job.MNK => "Monk",
            Job.DRG => "Dragoon",
            Job.NIN => "Ninja",
            Job.SAM => "Samurai",
            Job.RPR => "Reaper",

            Job.ARC => "Archer",
            Job.BRD => "Bard",
            Job.MCH => "Machinist",
            Job.DNC => "Dancer",

            Job.THM => "Thaumaturge",
            Job.ACN => "Arcanist",
            Job.BLM => "Black Mage",
            Job.SMN => "Summoner",
            Job.RDM => "Red Mage",
            Job.BLU => "Blue Mage",

            Job.CRP => "Carpenter",
            Job.BSM => "Blacksmith",
            Job.ARM => "Armorer",
            Job.GSM => "Goldsmith",
            Job.LTW => "Leatherworker",
            Job.WVR => "Weaver",
            Job.ALC => "Alchemist",
            Job.CUL => "Culinarian",

            Job.MIN => "Miner",
            Job.BOT => "Botanist",
            Job.FSH => "Fisher",

            _       => ""
        };
    }
}