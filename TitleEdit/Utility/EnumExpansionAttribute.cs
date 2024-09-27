using System;
using TitleEdit.Data.Lobby;

namespace TitleEdit.Utility
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class EnumExpansionAttribute(TitleScreenExpansion expansion) : Attribute
    {
        public TitleScreenExpansion expansion { get; init; } = expansion;
    }

}
