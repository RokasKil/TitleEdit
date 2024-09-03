using FFXIVClientStructs.FFXIV.Client.Game.Object;
using System.Numerics;

namespace TitleEdit.Extensions
{
    public static class GameObjectExtensions
    {
        public static void SetPosition(this ref GameObject gameobject, Vector3 position)
        {
            gameobject.SetPosition(position.X, position.Y, position.Z);
        }
    }
}
