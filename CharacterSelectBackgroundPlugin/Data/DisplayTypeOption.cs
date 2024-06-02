namespace CharacterSelectBackgroundPlugin.Data
{
    public struct DisplayTypeOption
    {
        public DisplayType type;
        public string? presetPath;
    }

    public enum DisplayType
    {
        LastLocation,
        Preset
    }
}
