using CharacterSelectBackgroundPlugin.Data.Lobby;
using CharacterSelectBackgroundPlugin.Data.Persistence;
using CharacterSelectBackgroundPlugin.Utility;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Common.Math;

namespace CharacterSelectBackgroundPlugin.PluginServices.Lobby
{
    public unsafe partial class LobbyService
    {
        private delegate void SetCameraCurveMidPointDelegate(LobbyCameraExpanded* self, float value);
        private delegate void CalculateCameraCurveLowAndHighPointDelegate(LobbyCameraExpanded* self, float value);
        private delegate void LobbySceneLoadedDelegate(ulong p1, int p2, float p3, ushort p4, uint p5, uint p6, uint p7);

        private Hook<SetCameraCurveMidPointDelegate> setCameraCurveMidPointHook = null!;
        private Hook<CalculateCameraCurveLowAndHighPointDelegate> calculateCameraCurveLowAndHighPointHook = null!;
        private Hook<LobbySceneLoadedDelegate> lobbySceneLoadedHook = null!;

        private float recordedYaw = 0;
        private float recordedPitch = 0;
        private float recordedDistance = 3.3f;
        private bool rotationJustRecorded;

        private float cameraYOffset = 0;

        private float lastLowPoint;
        private float lastMidPoint;

        private float lastHighPoint;
        private Vector3 lastLookAt;

        private bool cameraModified = false;

        private void HookCamera()
        {
            // Sets the middle point of the camera Y Position curve (made out of 3 points), called every frame, by default doesn't accept negative values, we fix that
            // and adjust by head offset so camera gets centered better
            setCameraCurveMidPointHook = Hook<SetCameraCurveMidPointDelegate>("0F 57 C0 0F 2F C1 73 ?? F3 0F 11 89", SetCameraCurveMidPointDetour);

            // Sets the low and high point of the camera Y Position curve, we adjust with head offset here so camera stays centered better
            calculateCameraCurveLowAndHighPointHook = Hook<CalculateCameraCurveLowAndHighPointDelegate>("F3 0F 10 81 ?? ?? ?? ?? F3 0F 11 89", CalculateCameraCurveLowAndHighPointDetour);

            // Some LobbySceneLoaded thingy, called once a new world level is loaded, we use it to restore camera Position
            lobbySceneLoadedHook = Hook<LobbySceneLoadedDelegate>("E8 ?? ?? ?? ?? 41 0F B7 CC C6 05", LobbySceneLoadedDetour);
        }

        private void CameraFollowCharacter(Character* currentChar)
        {
            var camera = GetCamera();
            if (camera != null)
            {

                var drawObject = (CharacterBase*)currentChar->GameObject.GetDrawObject();
                var cameraFollowMode = GetCameraFollowMode();
                Vector3 lookAt;
                if (drawObject != null && drawObject->DrawObject.IsVisible && cameraFollowMode == CameraFollowMode.ModelPosition)
                {
                    lookAt = drawObject->Skeleton->Transform.Position;
                }
                else
                {
                    lookAt = currentChar->GameObject.Position;
                }
                lookAt.Y = camera->LobbyCamera.Camera.CameraBase.SceneCamera.LookAtVector.Y;
                camera->LobbyCamera.Camera.CameraBase.SceneCamera.LookAtVector = OffsetPosition(lookAt);
                lastLowPoint = camera->LowPoint.Value;
                lastMidPoint = camera->MidPoint.Value;
                lastHighPoint = camera->HighPoint.Value;
                lastLookAt = camera->LobbyCamera.Camera.CameraBase.SceneCamera.LookAtVector;
            }
        }

        private void ResetLastCameraLookAtValues()
        {
            lastLowPoint = 1.4350828f + locationModel.Position.Y;
            lastMidPoint = 0.85870504f + locationModel.Position.Y;
            lastHighPoint = 0.6742642f + locationModel.Position.Y;
            lastLookAt = locationModel.Position;
        }

        private void CameraLookAtLastPosition()
        {
            var camera = GetCamera();
            if (camera != null)
            {
                camera->LowPoint.Value = lastLowPoint;
                camera->MidPoint.Value = lastMidPoint;
                camera->HighPoint.Value = lastHighPoint;
                camera->LobbyCamera.Camera.CameraBase.SceneCamera.LookAtVector = new(lastLookAt.X, camera->LobbyCamera.Camera.CameraBase.SceneCamera.LookAtVector.Y, lastLookAt.Z);
            }
        }

        private void ResetCameraLookAtOnExitCharacterSelect()
        {
            var camera = GetCamera();
            rotationJustRecorded = false;
            if (camera != null)
            {
                camera->LobbyCamera.Camera.CameraBase.SceneCamera.LookAtVector = Vector3.Zero;
            }
        }

        private void ModifyCamera()
        {
            var camera = GetCamera();
            if (camera != null)
            {
                camera->LobbyCamera.Camera.MaxDistance = 20;
                if (!cameraModified)
                {
                    camera->MidPoint.Position = 10;
                    camera->HighPoint.Position = 20;
                    cameraModified = true;
                }
            }
        }

        private void ClearCameraModifications()
        {
            if (cameraModified)
            {
                var camera = GetCamera();
                if (camera != null)
                {
                    camera->MidPoint.Position = 3.3f;
                    camera->HighPoint.Position = 5.5f;
                    camera->LobbyCamera.Camera.MaxDistance = 5.5f;
                    cameraModified = false;
                }
                rotationJustRecorded = false;
                recordedYaw = 0;
                recordedPitch = 0;
                recordedDistance = 3.3f;
            }
        }

        private void RecordCameraRotation()
        {
            // Prevent overwriting camera location when going through characters rapidly (switching while a scene is still loading)
            if (!rotationJustRecorded)
            {
                var camera = GetCamera();
                if (camera != null)
                {
                    recordedYaw = camera->Yaw;
                    recordedPitch = camera->Pitch;
                    recordedDistance = camera->LobbyCamera.Camera.Distance;
                    rotationJustRecorded = true;
                    if (CurrentCharacter != null)
                    {
                        recordedYaw -= CurrentCharacter->GameObject.Rotation;
                    }
                    recordedYaw = Utils.NormalizeAngle(recordedYaw);
                    Services.Log.Debug($"Recorded rotation {recordedYaw} {recordedPitch} {recordedDistance}");
                }
            }
        }

        private void LobbySceneLoadedDetour(ulong p1, int p2, float p3, ushort p4, uint p5, uint p6, uint p7)
        {
            Services.Log.Debug($"[LobbySceneLoaded] {p1:X} {p2:X} {p3} {p4:X} {p5:X} {p6:X} {p7:X}");
            lobbySceneLoadedHook.Original(p1, p2, p3, p4, p5, p6, p7);
            SetCameraRotation();
        }

        private void SetCameraRotation()
        {
            var camera = GetCamera();
            if (camera != null)
            {
                camera->Yaw = Utils.NormalizeAngle(recordedYaw);
                camera->Pitch = recordedPitch;
                camera->LobbyCamera.Camera.Distance = recordedDistance;
                camera->LobbyCamera.Camera.InterpDistance = recordedDistance;
            }
            rotationJustRecorded = false;
            Services.Log.Debug($"Loaded rotation {recordedYaw} {recordedPitch} {recordedDistance}");
            if (camera != null)
            {
                camera->Yaw = Utils.NormalizeAngle(camera->Yaw + locationModel.Rotation);
            }
            RotateCharacter();

            Services.Log.Debug($"After load rotation {camera->Yaw} {camera->Pitch} {camera->LobbyCamera.Camera.Distance}");
        }


        //SE's implenetation does nothing if Value is below 0 which breaks the camera when character is in negative Y
        private void SetCameraCurveMidPointDetour(LobbyCameraExpanded* self, float value)
        {
            if (GetCameraFollowMode() == CameraFollowMode.ModelPosition)
            {
                cameraYOffset = Services.BoneService.GetHeadOffset(CurrentCharacter);
            }
            else
            {
                cameraYOffset = 0;
            }
            self->MidPoint.Value = value + cameraYOffset;
        }

        private void CalculateCameraCurveLowAndHighPointDetour(LobbyCameraExpanded* self, float value)
        {
            calculateCameraCurveLowAndHighPointHook.Original(self, value + cameraYOffset);
        }

        private CameraFollowMode GetCameraFollowMode()
        {
            return locationModel.CameraFollowMode == CameraFollowMode.Inherit ? Services.ConfigurationService.CameraFollowMode : locationModel.CameraFollowMode; ;
        }

        //Can probably cache this?
        private LobbyCameraExpanded* GetCamera()
        {
            var cameraManager = FFXIVClientStructs.FFXIV.Client.Game.Control.CameraManager.Instance();
            return cameraManager != null ? (LobbyCameraExpanded*)cameraManager->LobbCamera : null;
        }

    }
}
