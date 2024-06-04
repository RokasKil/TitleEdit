namespace CharacterSelectBackgroundPlugin.Data.Persistence
{
    public struct DisplayTypeOption
    {
        public DisplayType Type;
        public string? PresetPath;
    }

    public enum DisplayType
    {
        LastLocation,
        Preset,
        AetherialSea
    }
}
