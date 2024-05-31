using CharacterSelectBackgroundPlugin.Utility;
using Dalamud.Hooking;
using System;
using System.Collections.Generic;
using static Dalamud.Plugin.Services.IGameInteropProvider;

namespace CharacterSelectBackgroundPlugin.PluginServices
{
    public abstract class AbstractService : IDisposable
    {
        private List<HookWrapper> hooks = new();

        protected Hook<T> Hook<T>(string signature, T detour, HookBackend backend = HookBackend.Automatic) where T : Delegate
        {
            var hook = Services.GameInteropProvider.HookFromSignature(signature, detour, backend);
            if (hook == null)
            {
                throw new Exception($"Failed to hook '{signature}'");
            }
            hooks.Add(new HookWrapper<T>(hook));
            return hook;
        }

        protected void EnableHooks() => hooks.ForEach(hook => hook.Enable());

        protected void DisableHooks() => hooks.ForEach(hook => hook.Disable());

        public virtual void Dispose() => hooks.ForEach(hook => hook.Dispose());

        protected abstract class HookWrapper : IDisposable
        {
            public abstract void Enable();
            public abstract void Disable();
            public abstract void Dispose();
        }

        private class HookWrapper<T> : HookWrapper where T : Delegate
        {
            public Hook<T> Hook { get; }

            public HookWrapper(Hook<T> hook)
            {
                Hook = hook;
            }


            public override void Enable() => Hook.Enable();

            public override void Disable() => Hook.Disable();

            public override void Dispose() => Hook.Dispose();
        }
    }
}
