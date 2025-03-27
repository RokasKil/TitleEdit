using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.Havok.Animation.Rig;
using static FFXIVClientStructs.Havok.Animation.Rig.hkaPose;

namespace TitleEdit.PluginServices
{
    public class BoneService : AbstractService
    {
        public BoneService() { }


        // Could cache stuff here?
        // but it doesn't take much to do these calculations every frame in character select screen
        public unsafe float GetHeadOffset(Character* character)
        {
            if (character == null) return 0;
            var drawObject = (CharacterBase*)character->GameObject.GetDrawObject();
            if (drawObject == null || !drawObject->DrawObject.IsVisible) return 0;
            var skeleton = drawObject->Skeleton;
            if (skeleton == null) return 0;

            var partial = drawObject->Skeleton->PartialSkeletons[1];
            var animatedPose = partial.GetHavokPose(0);
            var staticPose = partial.GetHavokPose(3);
            if (animatedPose == null || staticPose == null) return 0;
            var animatedHeadIdx = GetBoneIdx(animatedPose, "j_kao");
            var staticHeadIdx = GetBoneIdx(staticPose, "j_kao");
            if (animatedHeadIdx == -1 || staticHeadIdx == -1) return 0;

            var animatedHeadTransform = animatedPose->AccessBoneModelSpace(animatedHeadIdx, PropagateOrNot.DontPropagate);
            var staticHeadTransform = staticPose->AccessBoneModelSpace(staticHeadIdx, PropagateOrNot.DontPropagate);
            return (animatedHeadTransform->Translation.Y - staticHeadTransform->Translation.Y) * skeleton->Transform.Scale.Y;
        }

        private unsafe int GetBoneIdx(hkaPose* pose, string name)
        {
            var boneCount = pose->Skeleton->Bones.Length;
            for (int i = 0; i < boneCount; i++)
            {
                var rawBone = pose->Skeleton->Bones[i];
                if (rawBone.Name.String == name)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
