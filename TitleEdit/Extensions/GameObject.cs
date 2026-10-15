using FFXIVClientStructs.FFXIV.Client.Game.Object;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.Game.Character;

namespace TitleEdit.Extensions
{
    public static class GameObjectExtensions
    {
        public static void SetPosition(this ref Character character, Vector3 position)
        {
            character.SetPosition(position.X, position.Y, position.Z);
        }
    }
}