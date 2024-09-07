using System;

namespace TitleEdit.Utility
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class EnumTranslationAttribute(string translation, string? tooltip = null) : Attribute
    {
        public string Translation { get; init; } = translation;
        public string? Tooltip { get; init; } = tooltip;
    }

}
