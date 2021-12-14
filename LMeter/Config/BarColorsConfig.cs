using System.Numerics;
using ImGuiNET;
using LMeter.Helpers;

namespace LMeter.Config
{
    public class BarColorsConfig : IConfigPage
    {
        public string Name => "Colors";
        
        public IConfigPage GetDefault() => new BarColorsConfig();
        
        public ConfigColor PLDColor = new ConfigColor(168f / 255f, 210f / 255f, 230f / 255f, 1f);
        public ConfigColor DRKColor = new ConfigColor(209f / 255f, 38f / 255f, 204f / 255f, 1f);
        public ConfigColor WARColor = new ConfigColor(207f / 255f, 38f / 255f, 33f / 255f, 1f);
        public ConfigColor GNBColor = new ConfigColor(121f / 255f, 109f / 255f, 48f / 255f, 1f);
        public ConfigColor GLAColor = new ConfigColor(168f / 255f, 210f / 255f, 230f / 255f, 1f);
        public ConfigColor MRDColor = new ConfigColor(207f / 255f, 38f / 255f, 33f / 255f, 1f);

        public ConfigColor SCHColor = new ConfigColor(134f / 255f, 87f / 255f, 255f / 255f, 1f);
        public ConfigColor WHMColor = new ConfigColor(255f / 255f, 240f / 255f, 220f / 255f, 1f);
        public ConfigColor ASTColor = new ConfigColor(255f / 255f, 231f / 255f, 74f / 255f, 1f);
        public ConfigColor SGEColor = new ConfigColor(144f / 255f, 176f / 255f, 255f / 255f, 1f);
        public ConfigColor CNJColor = new ConfigColor(255f / 255f, 240f / 255f, 220f / 255f, 1f);

        public ConfigColor MNKColor = new ConfigColor(214f / 255f, 156f / 255f, 0f / 255f, 1f);
        public ConfigColor NINColor = new ConfigColor(175f / 255f, 25f / 255f, 100f / 255f, 1f);
        public ConfigColor DRGColor = new ConfigColor(65f / 255f, 100f / 255f, 205f / 255f, 1f);
        public ConfigColor SAMColor = new ConfigColor(228f / 255f, 109f / 255f, 4f / 255f, 1f);
        public ConfigColor RPRColor = new ConfigColor(150f / 255f, 90f / 255f, 144f / 255f, 1f);
        public ConfigColor PGLColor = new ConfigColor(214f / 255f, 156f / 255f, 0f / 255f, 1f);
        public ConfigColor ROGColor = new ConfigColor(175f / 255f, 25f / 255f, 100f / 255f, 1f);
        public ConfigColor LNCColor = new ConfigColor(65f / 255f, 100f / 255f, 205f / 255f, 1f);
        
        public ConfigColor BRDColor = new ConfigColor(145f / 255f, 186f / 255f, 94f / 255f, 1f);
        public ConfigColor MCHColor = new ConfigColor(110f / 255f, 225f / 255f, 214f / 255f, 1f);
        public ConfigColor DNCColor = new ConfigColor(226f / 255f, 176f / 255f, 175f / 255f, 1f);
        public ConfigColor ARCColor = new ConfigColor(145f / 255f, 186f / 255f, 94f / 255f, 1f);

        public ConfigColor BLMColor = new ConfigColor(165f / 255f, 121f / 255f, 214f / 255f, 1f);
        public ConfigColor SMNColor = new ConfigColor(45f / 255f, 155f / 255f, 120f / 255f, 1f);
        public ConfigColor RDMColor = new ConfigColor(232f / 255f, 123f / 255f, 123f / 255f, 1f);
        public ConfigColor BLUColor = new ConfigColor(0f / 255f, 185f / 255f, 247f / 255f, 1f);
        public ConfigColor THMColor = new ConfigColor(165f / 255f, 121f / 255f, 214f / 255f, 1f);
        public ConfigColor ACNColor = new ConfigColor(45f / 255f, 155f / 255f, 120f / 255f, 1f);

        public ConfigColor UKNColor = new ConfigColor(218f / 255f, 157f / 255f, 46f / 255f, 1f);

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

            Job.ARC => this.ARCColor,
            Job.BRD => this.BRDColor,
            Job.MCH => this.MCHColor,
            Job.DNC => this.DNCColor,

            Job.THM => this.THMColor,
            Job.ACN => this.ACNColor,
            Job.BLM => this.BLMColor,
            Job.SMN => this.SMNColor,
            Job.RDM => this.RDMColor,
            Job.BLU => this.BLUColor,
            _       => this.UKNColor
        };

        public void DrawConfig(Vector2 size, float padX, float padY)
        {
            if (ImGui.BeginChild($"##{this.Name}", new Vector2(size.X, size.Y), true))
            {
                Vector4 vector = PLDColor.Vector;
                ImGui.ColorEdit4("PLD", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
                this.PLDColor.Vector = vector;
                
                vector = WARColor.Vector;
                ImGui.ColorEdit4("WAR", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
                this.WARColor.Vector = vector;
                vector = DRKColor.Vector;
                ImGui.ColorEdit4("DRK", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
                this.DRKColor.Vector = vector;
                
                vector = GNBColor.Vector;
                ImGui.ColorEdit4("GNB", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
                this.GNBColor.Vector = vector;

                ImGui.NewLine();
                
                vector = SCHColor.Vector;
                ImGui.ColorEdit4("SCH", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
                this.SCHColor.Vector = vector;
                
                vector = WHMColor.Vector;
                ImGui.ColorEdit4("WHM", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
                this.WHMColor.Vector = vector;
                
                vector = ASTColor.Vector;
                ImGui.ColorEdit4("AST", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
                this.ASTColor.Vector = vector;
                
                vector = SGEColor.Vector;
                ImGui.ColorEdit4("SGE", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
                this.SGEColor.Vector = vector;

                ImGui.NewLine();
                
                vector = MNKColor.Vector;
                ImGui.ColorEdit4("MNK", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
                this.MNKColor.Vector = vector;
                
                vector = NINColor.Vector;
                ImGui.ColorEdit4("NIN", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
                this.NINColor.Vector = vector;
                
                vector = DRGColor.Vector;
                ImGui.ColorEdit4("DRG", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
                this.DRGColor.Vector = vector;
                
                vector = SAMColor.Vector;
                ImGui.ColorEdit4("SAM", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
                this.SAMColor.Vector = vector;
                
                vector = RPRColor.Vector;
                ImGui.ColorEdit4("RPR", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
                this.RPRColor.Vector = vector;

                ImGui.NewLine();
                
                vector = BRDColor.Vector;
                ImGui.ColorEdit4("BRD", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
                this.BRDColor.Vector = vector;
                
                vector = MCHColor.Vector;
                ImGui.ColorEdit4("MCH", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
                this.MCHColor.Vector = vector;
                
                vector = DNCColor.Vector;
                ImGui.ColorEdit4("DNC", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
                this.DNCColor.Vector = vector;

                ImGui.NewLine();
                
                vector = BLMColor.Vector;
                ImGui.ColorEdit4("BLM", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
                this.BLMColor.Vector = vector;
                
                vector = SMNColor.Vector;
                ImGui.ColorEdit4("SMN", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
                this.SMNColor.Vector = vector;
                
                vector = RDMColor.Vector;
                ImGui.ColorEdit4("RDM", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
                this.RDMColor.Vector = vector;
                
                vector = BLUColor.Vector;
                ImGui.ColorEdit4("BLU", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
                this.BLUColor.Vector = vector;

                ImGui.NewLine();
                
                vector = GLAColor.Vector;
                ImGui.ColorEdit4("GLA", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
                this.GLAColor.Vector = vector;
                
                vector = MRDColor.Vector;
                ImGui.ColorEdit4("MRD", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
                this.MRDColor.Vector = vector;
                
                vector = CNJColor.Vector;
                ImGui.ColorEdit4("CNJ", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
                this.CNJColor.Vector = vector;
                
                vector = PGLColor.Vector;
                ImGui.ColorEdit4("PGL", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
                this.PGLColor.Vector = vector;
                
                vector = ROGColor.Vector;
                ImGui.ColorEdit4("ROG", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
                this.ROGColor.Vector = vector;
                
                vector = LNCColor.Vector;
                ImGui.ColorEdit4("LNC", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
                this.LNCColor.Vector = vector;
                
                vector = ARCColor.Vector;
                ImGui.ColorEdit4("ARC", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
                this.ARCColor.Vector = vector;
                
                vector = THMColor.Vector;
                ImGui.ColorEdit4("THM", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
                this.THMColor.Vector = vector;
                
                vector = ACNColor.Vector;
                ImGui.ColorEdit4("ACN", ref vector, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
                this.ACNColor.Vector = vector;
            }

            ImGui.EndChild();
        }
    }
}
