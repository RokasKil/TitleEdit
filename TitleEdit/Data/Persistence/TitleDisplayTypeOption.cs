namespace TitleEdit.Data.Persistence
{
    public struct TitleDisplayTypeOption : IPresetContainer
    {
        public TitleDisplayType Type;
        public string? PresetPath { get; set; }

    }

    public enum TitleDisplayType
    {
        Preset,
        Random
    }
}
