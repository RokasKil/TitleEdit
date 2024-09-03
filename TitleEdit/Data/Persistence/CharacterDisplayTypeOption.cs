using System;

namespace TitleEdit.Data.Persistence
{
    public struct CharacterDisplayTypeOption : IPresetContainer
    {
        public CharacterDisplayType Type;
        public string? PresetPath { get; set; }
    }

    public enum CharacterDisplayType
    {
        LastLocation,
        Preset,
        [Obsolete]
        AetherialSea,
        Random
    }
}
