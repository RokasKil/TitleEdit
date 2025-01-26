using System.Numerics;

namespace TitleEdit.Data.Persistence;

public struct NpcModel
{
    public uint ENpcId;
    public Vector3 Position;
    public float Rotation;
    public float Scale = 1f;

    public NpcModel() { }
}
