using System.Runtime.InteropServices;

namespace TitleEdit.Data.Character
{
    [StructLayout(LayoutKind.Explicit, Size = 0x1BD0)]
    public unsafe struct CharacterExpanded
    {
        [FieldOffset(0x0)] public FFXIVClientStructs.FFXIV.Client.Game.Character.Character Character;
        [FieldOffset(0x61c)] public MovementMode MovementMode;
    }
    public enum MovementMode : byte
    {
        Normal = 0,
        Flying = 1,
        Swiming = 2
    }
}
