using Dalamud.Configuration;
using System;
using System.Collections.Generic;

namespace TitleEdit.Data.Persistence.Migration
{
    public enum OverrideSetting
    {
        Override,
        UseIfUnspecified
    }

    [Serializable]
    public class TitleEditV2Configuration : IPluginConfiguration
    {
        public List<string> TitleList { get; set; } = new();
        public string SelectedTitleFileName { get; set; } = "Dawntrail";
        public string SelectedLogoName { get; set; } = "Dawntrail";
        public bool DisplayTitleLogo { get; set; } = true;
        public bool DisplayVersionText { get; set; } = true;
        public OverrideSetting Override { get; set; } = OverrideSetting.UseIfUnspecified;
        public OverrideSetting VisibilityOverride { get; set; } = OverrideSetting.UseIfUnspecified;
        public bool DisplayTitleToast { get; set; }
        public bool DebugLogging { get; set; }
        public int Version { get; set; }
    }
}
