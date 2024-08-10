using CharacterSelectBackgroundPlugin.Data.Camera;
using FFXIVClientStructs.FFXIV.Client.Game.Control;

namespace CharacterSelectBackgroundPlugin.PluginServices
{
    public class CameraService : AbstractService
    {
        public unsafe CameraManager* CameraManager => FFXIVClientStructs.FFXIV.Client.Game.Control.CameraManager.Instance();
        public unsafe LobbyCameraExpanded* LobbyCamera => (LobbyCameraExpanded*)CameraManager->LobbCamera;
        public unsafe CameraExpanded* CurrentCamera => (CameraExpanded*)CameraManager->GetActiveCamera();

        public CameraService()
        {

        }

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
