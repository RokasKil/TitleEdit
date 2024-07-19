using CharacterSelectBackgroundPlugin.Data.Layout;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;

namespace CharacterSelectBackgroundPlugin.Extensions
{
    public static class LayoutInstance
    {
        public static ulong UUID(this ILayoutInstance layoutInstance) => layoutInstance.Id.InstanceKey + ((ulong)layoutInstance.SubId << 32);
        public static unsafe void SetActiveVf54(this ref ILayoutInstance layoutInstance, bool active)
        {
            fixed (void* ptr = &layoutInstance)
            {
                var vtbl = *(ILayoutInstanceVTable**)ptr;
                vtbl->setActiveVF54(ptr, active);
            }
        }

        // I think it's duration I don't remember actually
        public static unsafe void SetActiveVf41(this ref ILayoutInstance layoutInstance, bool active, float fadeDuration)
        {
            fixed (void* ptr = &layoutInstance)
            {
                var vtbl = *(ILayoutInstanceVTable**)ptr;
                vtbl->setActiveVF41(ptr, active, fadeDuration);
            }
        }


        public static bool IsActive(this ref ILayoutInstance layoutInstance) => (layoutInstance.Flags3 & 0b10000) != 0;
    }
}
