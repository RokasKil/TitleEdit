using FFXIVClientStructs.FFXIV.Client.Game.Character;
using System.Runtime.InteropServices;

namespace CharacterSelectBackgroundPlugin.Data
{
    [StructLayout(LayoutKind.Explicit, Size = 0x1BD0)]
    public unsafe partial struct CharacterExpanded
    {
        [FieldOffset(0x0)] public Character Character;
        [FieldOffset(0x60c)] public MovementMode movementMode;
        public enum MovementMode : byte
        {
            Normal = 0,
            Flying = 1,
            Swiming = 2
        }
    }
}
