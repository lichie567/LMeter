using System.Numerics;
using System.Text.Json.Serialization;
using ImGuiNET;
using LMeter.Helpers;

namespace LMeter.Config
{
    public class BarColorsConfig : IConfigPage
    {
        [JsonIgnore]
        public bool Active { get; set; }
        
        public string Name => "Colors";

        public IConfigPage GetDefault() => new BarColorsConfig();

        public ConfigColor PLDColor = new(168f / 255f, 210f / 255f, 230f / 255f, 1f);
        public ConfigColor DRKColor = new(209f / 255f, 38f / 255f, 204f / 255f, 1f);
        public ConfigColor WARColor = new(207f / 255f, 38f / 255f, 33f / 255f, 1f);
        public ConfigColor GNBColor = new(121f / 255f, 109f / 255f, 48f / 255f, 1f);
        public ConfigColor GLAColor = new(168f / 255f, 210f / 255f, 230f / 255f, 1f);
        public ConfigColor MRDColor = new(207f / 255f, 38f / 255f, 33f / 255f, 1f);

        public ConfigColor SCHColor = new(134f / 255f, 87f / 255f, 255f / 255f, 1f);
        public ConfigColor WHMColor = new(255f / 255f, 240f / 255f, 220f / 255f, 1f);
        public ConfigColor ASTColor = new(255f / 255f, 231f / 255f, 74f / 255f, 1f);
        public ConfigColor SGEColor = new(144f / 255f, 176f / 255f, 255f / 255f, 1f);
        public ConfigColor CNJColor = new(255f / 255f, 240f / 255f, 220f / 255f, 1f);

        public ConfigColor MNKColor = new(214f / 255f, 156f / 255f, 0f / 255f, 1f);
        public ConfigColor NINColor = new(175f / 255f, 25f / 255f, 100f / 255f, 1f);
        public ConfigColor DRGColor = new(65f / 255f, 100f / 255f, 205f / 255f, 1f);
        public ConfigColor SAMColor = new(228f / 255f, 109f / 255f, 4f / 255f, 1f);
        public ConfigColor RPRColor = new(150f / 255f, 90f / 255f, 144f / 255f, 1f);
        public ConfigColor VPRColor = new(16f / 255f, 130f / 255f, 16f / 255f, 1f);
        public ConfigColor PGLColor = new(214f / 255f, 156f / 255f, 0f / 255f, 1f);
        public ConfigColor ROGColor = new(175f / 255f, 25f / 255f, 100f / 255f, 1f);
        public ConfigColor LNCColor = new(65f / 255f, 100f / 255f, 205f / 255f, 1f);

        public ConfigColor BRDColor = new(145f / 255f, 186f / 255f, 94f / 255f, 1f);
        public ConfigColor MCHColor = new(110f / 255f, 225f / 255f, 214f / 255f, 1f);
        public ConfigColor DNCColor = new(226f / 255f, 176f / 255f, 175f / 255f, 1f);
        public ConfigColor ARCColor = new(145f / 255f, 186f / 255f, 94f / 255f, 1f);

        public ConfigColor BLMColor = new(165f / 255f, 121f / 255f, 214f / 255f, 1f);
        public ConfigColor SMNColor = new(45f / 255f, 155f / 255f, 120f / 255f, 1f);
        public ConfigColor RDMColor = new(232f / 255f, 123f / 255f, 123f / 255f, 1f);
        public ConfigColor PCTColor = new(252f / 255f, 146f / 255f, 225f / 255f, 1f);
        public ConfigColor BLUColor = new(0f / 255f, 185f / 255f, 247f / 255f, 1f);
        public ConfigColor THMColor = new(165f / 255f, 121f / 255f, 214f / 255f, 1f);
        public ConfigColor ACNColor = new(45f / 255f, 155f / 255f, 120f / 255f, 1f);

        public ConfigColor UKNColor = new(218f / 255f, 157f / 255f, 46f / 255f, 1f);

        public ConfigColor GetColor(Job job) => job switch
        {
            Job.GLA => this.GLAColor,
            Job.MRD => this.MRDColor,
            Job.PLD => this.PLDColor,
            Job.WAR => this.WARColor,
            Job.DRK => this.DRKColor,
            Job.GNB => this.GNBColor,

            Job.CNJ => this.CNJColor,
            Job.WHM => this.WHMColor,
            Job.SCH => this.SCHColor,
            Job.AST => this.ASTColor,
            Job.SGE => this.SGEColor,

            Job.PGL => this.PGLColor,
            Job.LNC => this.LNCColor,
            Job.ROG => this.ROGColor,
            Job.MNK => this.MNKColor,
            Job.DRG => this.DRGColor,
            Job.NIN => this.NINColor,
            Job.SAM => this.SAMColor,
            Job.RPR => this.RPRColor,
            Job.VPR => this.VPRColor,

            Job.ARC => this.ARCColor,
            Job.BRD => this.BRDColor,
            Job.MCH => this.MCHColor,
            Job.DNC => this.DNCColor,

            Job.THM => this.THMColor,
            Job.ACN => this.ACNColor,
            Job.BLM => this.BLMColor,
            Job.SMN => this.SMNColor,
            Job.RDM => this.RDMColor,
            Job.PCT => this.PCTColor,
            Job.BLU => this.BLUColor,
            _       => this.UKNColor
        };

        public void DrawConfig(Vector2 size, float padX, float padY, bool border = true)
        {
            if (ImGui.BeginChild($"##{this.Name}", new Vector2(size.X, size.Y), border))
            {
                DrawHelpers.DrawColorSelector("PLD", ref this.PLDColor);
                DrawHelpers.DrawColorSelector("WAR", ref this.WARColor);
                DrawHelpers.DrawColorSelector("DRK", ref this.DRKColor);
                DrawHelpers.DrawColorSelector("GNB", ref this.GNBColor);

                ImGui.NewLine();
                DrawHelpers.DrawColorSelector("SCH", ref this.SCHColor);
                DrawHelpers.DrawColorSelector("WHM", ref this.WHMColor);
                DrawHelpers.DrawColorSelector("AST", ref this.ASTColor);
                DrawHelpers.DrawColorSelector("SGE", ref this.SGEColor);

                ImGui.NewLine();
                DrawHelpers.DrawColorSelector("MNK", ref this.MNKColor);
                DrawHelpers.DrawColorSelector("NIN", ref this.NINColor);
                DrawHelpers.DrawColorSelector("DRG", ref this.DRGColor);
                DrawHelpers.DrawColorSelector("SAM", ref this.SAMColor);
                DrawHelpers.DrawColorSelector("RPR", ref this.RPRColor);
                DrawHelpers.DrawColorSelector("VPR", ref this.VPRColor);

                ImGui.NewLine();
                DrawHelpers.DrawColorSelector("BRD", ref this.BRDColor);
                DrawHelpers.DrawColorSelector("MCH", ref this.MCHColor);
                DrawHelpers.DrawColorSelector("DNC", ref this.DNCColor);

                ImGui.NewLine();
                DrawHelpers.DrawColorSelector("BLM", ref this.BLMColor);
                DrawHelpers.DrawColorSelector("SMN", ref this.SMNColor);
                DrawHelpers.DrawColorSelector("RDM", ref this.RDMColor);
                DrawHelpers.DrawColorSelector("PCT", ref this.PCTColor);
                DrawHelpers.DrawColorSelector("BLU", ref this.BLUColor);

                ImGui.NewLine();
                DrawHelpers.DrawColorSelector("GLA", ref this.GLAColor);
                DrawHelpers.DrawColorSelector("MRD", ref this.MRDColor);
                DrawHelpers.DrawColorSelector("CNJ", ref this.CNJColor);
                DrawHelpers.DrawColorSelector("PGL", ref this.PGLColor);
                DrawHelpers.DrawColorSelector("ROG", ref this.ROGColor);
                DrawHelpers.DrawColorSelector("LNC", ref this.LNCColor);
                DrawHelpers.DrawColorSelector("ARC", ref this.ARCColor);
                DrawHelpers.DrawColorSelector("THM", ref this.THMColor);
                DrawHelpers.DrawColorSelector("ACN", ref this.ACNColor);

                ImGui.NewLine();
                DrawHelpers.DrawColorSelector("Limit Break", ref this.UKNColor);
            }

            ImGui.EndChild();
        }
    }
}
