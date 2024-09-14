using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Common.Math;
using System;
using TitleEdit.Data.Camera;
using TitleEdit.Data.Lobby;
using TitleEdit.Data.Persistence;
using TitleEdit.Utility;

namespace TitleEdit.PluginServices.Lobby
{
    public unsafe partial class LobbyService
    {
        private delegate void SetCameraCurveMidPointDelegate(LobbyCameraExpanded* self, float value);
        private delegate void CalculateCameraCurveLowAndHighPointDelegate(LobbyCameraExpanded* self, float value);
        private delegate void LobbySceneLoadedDelegate(ulong p1, int p2, float p3, ushort p4, uint p5, uint p6, uint p7);
        private delegate void LobbyCameraFixOnDelegate(LobbyCameraExpanded* self, Vector3 cameraPos, Vector3 focusPos, float fovY);
        private delegate float CalculateLobbyCameraLookAtYDelegate(LobbyCameraExpanded* self, float distance, CurvePoint* lowPoint, CurvePoint* midPoint, CurvePoint* highPoint);

        private Hook<SetCameraCurveMidPointDelegate> setCameraCurveMidPointHook = null!;
        private Hook<CalculateCameraCurveLowAndHighPointDelegate> calculateCameraCurveLowAndHighPointHook = null!;
        private Hook<LobbySceneLoadedDelegate> lobbySceneLoadedHook = null!;
        private Hook<LobbyCameraFixOnDelegate> lobbyCameraFixOnHook = null!;
        private Hook<CalculateLobbyCameraLookAtYDelegate> calculateLobbyCameraLookAtYHook = null!;

        private CameraFollowMode CameraFollowMode => characterSelectLocationModel.CameraFollowMode == CameraFollowMode.Inherit ?
            Services.ConfigurationService.CameraFollowMode :
            characterSelectLocationModel.CameraFollowMode;

        private LobbyCameraExpanded* LobbyCamera => (LobbyCameraExpanded*)(FFXIVClientStructs.FFXIV.Client.Game.Control.CameraManager.Instance()->LobbCamera);

        // Recorded angles to reload when changing scenes in character select
        private float recordedYaw = 0;
        private float recordedPitch = 0;
        private float recordedDistance = 3.3f;
        private float previousCharacterSelectModelRotation = 0f;

        //default state is recorded so we don't record anything when entering character select
        private bool rotationJustRecorded = true;

        // Used when just loaded a new scene in character select and want to snap the camera's Y
        private bool shouldSetLookAtY = false;

        // character select lookAt Y value offset calculated based on player models head position relative to it's regular position
        private float cameraYOffset = 0;

        // Last curve and lookAt positions for use when nothing is selected
        private float lastLowPoint;
        private float lastMidPoint;
        private float lastHighPoint;
        private Vector3 lastLookAt;

        // Is lobby camera max zoom modified
        private bool cameraModified = false;

        private void HookCamera()
        {
            // Sets the middle point of the camera Y Position curve (made out of 3 points), called every frame, by default doesn't accept negative values, we fix that
            // and adjust by head offset so camera gets centered better
            setCameraCurveMidPointHook = Hook<SetCameraCurveMidPointDelegate>("0F 57 C0 0F 2F C1 73 ?? F3 0F 11 89", SetCameraCurveMidPointDetour);

            // Sets the low and high point of the camera Y Position curve, we adjust with head offset here so camera stays centered better
            calculateCameraCurveLowAndHighPointHook = Hook<CalculateCameraCurveLowAndHighPointDelegate>("F3 0F 10 81 ?? ?? ?? ?? F3 0F 11 89", CalculateCameraCurveLowAndHighPointDetour);

            // Some LobbySceneLoaded thingy, called once a new world level is loaded, we use it to restore camera Position
            lobbySceneLoadedHook = Hook<LobbySceneLoadedDelegate>("E8 ?? ?? ?? ?? 41 0F B7 CE 40 88 2D", LobbySceneLoadedDetour);

            // Called when the game needs to set LookAt of the lobbyCamera but we override that every frame so it's not needed for that purpose
            // We use this to additionaly override camera position cause it will set stuff to 0, 0, 0 and if the title screen and character select use the same level
            // this ight cause the loud sound bug
            lobbyCameraFixOnHook = Hook<LobbyCameraFixOnDelegate>("E8 ?? ?? ?? ?? 89 9C 24 ?? ?? ?? ?? E8", LobbyCameraFixOnDetour);

            calculateLobbyCameraLookAtYHook = Hook<CalculateLobbyCameraLookAtYDelegate>("48 83 EC ?? F3 41 0F 10 01 0F 28 D1", calculateLobbyCameraLookAtYDetour);
        }

        private void CameraTick()
        {
            LobbyCamera->LobbyCamera.Camera.CameraBase.SceneCamera.Vector_1 = new(0, 1, 0);
            if (CurrentLobbyMap == GameLobbyType.CharaSelect)
            {
                if (CurrentCharacter != null)
                {
                    // Tell camera to follow the character
                    CameraFollowCharacter(CurrentCharacter);
                }
                else
                {
                    // Tell camera to look at last recorded position
                    CameraLookAtLastPosition();
                }
                if (shouldSetLookAtY)
                {
                    // Reset camera lookAt Y position
                    shouldSetLookAtY = false;
                    ForceSetLookAtY();
                }

            }
            else if (CurrentLobbyMap == GameLobbyType.Title)
            {
                if (ShouldModifyTitleScreen)
                {
                    // Set camera positions for title screen
                    CameraSetTitleScreenPosition();
                }
            }
        }

        // Sets LookAt and Curve values for character depending on current CameraFollowMode
        private void CameraFollowCharacter(Character* currentChar)
        {
            var drawObject = (CharacterBase*)currentChar->GameObject.GetDrawObject();
            Vector3 lookAt;
            if (drawObject != null && drawObject->DrawObject.IsVisible && CameraFollowMode == CameraFollowMode.ModelPosition)
            {
                lookAt = drawObject->Skeleton->Transform.Position;
            }
            else
            {
                lookAt = currentChar->GameObject.Position;
            }
            lookAt.Y = LobbyCamera->LobbyCamera.Camera.CameraBase.SceneCamera.LookAtVector.Y;
            LobbyCamera->LobbyCamera.Camera.CameraBase.SceneCamera.LookAtVector = OffsetPosition(lookAt);
            lastLowPoint = LobbyCamera->LowPoint.Value;
            lastMidPoint = LobbyCamera->MidPoint.Value;
            lastHighPoint = LobbyCamera->HighPoint.Value;
            lastLookAt = LobbyCamera->LobbyCamera.Camera.CameraBase.SceneCamera.LookAtVector;

        }

        // Sets default LookAt and Curve values for currently loaded chracter select location
        private void ResetLastCameraLookAtValues()
        {
            lastLowPoint = 1.4350828f + characterSelectLocationModel.Position.Y;
            lastMidPoint = 0.85870504f + characterSelectLocationModel.Position.Y;
            lastHighPoint = 0.6742642f + characterSelectLocationModel.Position.Y;
            lastLookAt = characterSelectLocationModel.Position;
        }

        // Sets Character select nothing selected LookAt vector
        private void CameraLookAtLastPosition()
        {
            LobbyCamera->LowPoint.Value = lastLowPoint;
            LobbyCamera->MidPoint.Value = lastMidPoint;
            LobbyCamera->HighPoint.Value = lastHighPoint;
            LobbyCamera->LobbyCamera.Camera.CameraBase.SceneCamera.LookAtVector = new(lastLookAt.X, LobbyCamera->LobbyCamera.Camera.CameraBase.SceneCamera.LookAtVector.Y, lastLookAt.Z);
        }

        // Sets TitleScreen coordinates and angles
        private void CameraSetTitleScreenPosition()
        {
            var lookAt = Utils.GetVectorFromAngles(titleScreenLocationModel.Yaw, titleScreenLocationModel.Pitch);
            LobbyCamera->LobbyCamera.Camera.CameraBase.SceneCamera.Position = titleScreenLocationModel.CameraPosition;
            LobbyCamera->LobbyCamera.Camera.CameraBase.SceneCamera.LookAtVector = OffsetPosition(titleScreenLocationModel.CameraPosition + lookAt);
            LobbyCamera->LobbyCamera.Camera.CameraBase.SceneCamera.RenderCamera->FoV = titleScreenLocationModel.Fov;
            // Up vector
            // A lot of this vector math is above me so something might be wrong here but it works fine from my testing
            Quaternion rotation = Quaternion.CreateFromAxisAngle(lookAt, titleScreenLocationModel.Roll);
            Vector3 rotatedUpVector = Vector3.Transform(Utils.GetVectorFromAngles(titleScreenLocationModel.Yaw, titleScreenLocationModel.Pitch + MathF.PI / 2), rotation);
            LobbyCamera->LobbyCamera.Camera.CameraBase.SceneCamera.Vector_1 = rotatedUpVector;
        }

        private void ResetCameraLookAtOnExitCharacterSelect()
        {
            ResetCameraRecordedRotation();
            LobbyCamera->LobbyCamera.Camera.CameraBase.SceneCamera.LookAtVector = Vector3.Zero;
        }

        // Used to reset LookAt vector's Y value in character select so the camera doesn't slide in vertically from previous position
        // Does some simpleish math which and I would rather call the native code for than rewrite into c#
        private void ForceSetLookAtY()
        {
            LobbyCamera->LobbyCamera.Camera.SceneCamera.LookAtVector.Y = calculateLobbyCameraLookAtYDetour(
                LobbyCamera,
                LobbyCamera->LobbyCamera.Distance,
                &LobbyCamera->LowPoint,
                &LobbyCamera->MidPoint,
                &LobbyCamera->HighPoint);
            Services.Log.Debug($"Set lookAtVectorY to {LobbyCamera->LobbyCamera.Camera.SceneCamera.LookAtVector.Y} {LobbyCamera->LowPoint.Value} {LobbyCamera->MidPoint.Value} {LobbyCamera->HighPoint.Value}");
        }

        private float calculateLobbyCameraLookAtYDetour(LobbyCameraExpanded* self, float distance, CurvePoint* lowPoint, CurvePoint* midPoint, CurvePoint* highPoint)
        {
            // Clamp the distance value to the highpoint to keep vanilla curve and avoid weird behaviour when going past it with extended zoom
            return calculateLobbyCameraLookAtYHook.Original(self, MathF.Min(distance, highPoint->Position), lowPoint, midPoint, highPoint);
        }


        //Expands lobby camera max distance in case the user is using a big mount
        private void ModifyCamera()
        {
            LobbyCamera->LobbyCamera.Camera.MaxDistance = 20;
            cameraModified = true;
        }

        //Restores default camera properties
        private void ClearCameraModifications()
        {
            if (cameraModified)
            {
                LobbyCamera->LobbyCamera.Camera.MaxDistance = 5.5f;
                cameraModified = false;

                ResetCameraRecordedRotation();
            }
        }

        // Record camera angles to restore it after a scene loads
        private void RecordCameraRotation()
        {
            // Prevent overwriting camera location when going through characters rapidly (switching while a scene is still loading)
            if (!rotationJustRecorded)
            {
                recordedYaw = Utils.NormalizeAngle(LobbyCamera->Yaw - previousCharacterSelectModelRotation);
                recordedPitch = LobbyCamera->Pitch;
                recordedDistance = LobbyCamera->LobbyCamera.Camera.Distance;
                rotationJustRecorded = true;
                Services.Log.Debug($"Recorded rotation {recordedYaw} {recordedPitch} {recordedDistance} ({lastCharacterRotation} {LobbyCamera->Yaw - lastCharacterRotation} {Utils.NormalizeAngle(LobbyCamera->Yaw - lastCharacterRotation)})");
            }
        }

        // Is called when a scene is loaded I think
        private void LobbySceneLoadedDetour(ulong p1, int p2, float p3, ushort p4, uint p5, uint p6, uint p7)
        {
            Services.Log.Debug($"[LobbySceneLoaded] {p1:X} {p2:X} {p3} {p4:X} {p5:X} {p6:X} {p7:X}");
            lobbySceneLoadedHook.Original(p1, p2, p3, p4, p5, p6, p7);
            if (CurrentLobbyMap == GameLobbyType.CharaSelect)
            {
                SetCameraRotation();
                ShowToastNotification(characterSelectLocationModel.ToastNotificationText);
            }
        }

        // Reset recorded angles when leaving character select
        private void ResetCameraRecordedRotation()
        {
            rotationJustRecorded = true;
            recordedYaw = 0;
            recordedPitch = 0;
            recordedDistance = 3.3f;
            lastCharacterRotation = 0;
        }

        // Restore recoreded camera angles
        private void SetCameraRotation()
        {
            LobbyCamera->Yaw = Utils.NormalizeAngle(recordedYaw + characterSelectLocationModel.Rotation);
            LobbyCamera->Pitch = recordedPitch;
            LobbyCamera->LobbyCamera.Camera.Distance = recordedDistance;
            LobbyCamera->LobbyCamera.Camera.InterpDistance = recordedDistance;

            rotationJustRecorded = false;

            RotateCharacter();

            Services.Log.Debug($"After load rotation {LobbyCamera->Yaw} {LobbyCamera->Pitch} {LobbyCamera->LobbyCamera.Camera.Distance}");
            shouldSetLookAtY = true;
        }


        //SE's implenetation does nothing if Value is below 0 which breaks the camera when character is in negative Y
        private void SetCameraCurveMidPointDetour(LobbyCameraExpanded* self, float value)
        {
            if (CameraFollowMode == CameraFollowMode.ModelPosition)
            {
                cameraYOffset = Services.BoneService.GetHeadOffset(CurrentCharacter);
            }
            else
            {
                cameraYOffset = 0;
            }
            //Services.Log.Debug($"SetCameraCurveMidPointDetour {value}");
            self->MidPoint.Value = value + cameraYOffset;
        }

        private void CalculateCameraCurveLowAndHighPointDetour(LobbyCameraExpanded* self, float value)
        {
            calculateCameraCurveLowAndHighPointHook.Original(self, value + cameraYOffset);
        }

        // Called when camera locks on a specific point in title screen
        // We use this to aditionally call CameraTick and reset that value to whatver it needs to be to prevent the 0 coordinage bug
        private void LobbyCameraFixOnDetour(LobbyCameraExpanded* self, Vector3 cameraPos, Vector3 focusPos, float fovY)
        {
            Services.Log.Debug($"LobbyCameraFixOnDetour {(IntPtr)self:X}, {self == LobbyCamera}, {cameraPos}, {focusPos}, {fovY}");
            lobbyCameraFixOnHook.Original(self, cameraPos, focusPos, fovY);
            CameraTick();
        }
    }
}
