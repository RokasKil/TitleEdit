using System;

namespace CharacterSelectBackgroundPlugin.Utility
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class EnumTranslationAttribute(string translation) : Attribute
    {
        public string Translation { get; init; } = translation;
    }

}
