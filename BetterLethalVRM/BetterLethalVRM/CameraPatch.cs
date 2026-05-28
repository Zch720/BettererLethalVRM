using System;
using UnityEngine;
using UnityEngine.InputSystem;
using HarmonyLib;
using GameNetcodeStuff;

namespace OomJan.BetterLethalVRM;

[HarmonyPatch(typeof(PlayerControllerB))]
public static class CameraPatch
{
    private static float _freeLookH = 0f;
    private static float _freeLookV = 0f;
    private static bool _wasRestricted = false;
    private static Vector3 _entryLocalEuler;
    private static Vector3 _entryLocalPos;

    internal static void Initialize() { }
    internal static void Cleanup() { }

    [HarmonyPatch("LateUpdate")]
    [HarmonyPostfix]
    public static void LateUpdatePostfix(PlayerControllerB __instance)
    {
        // Wrap the whole body so a transient NRE during death/respawn doesn't spam the log
        // every frame and doesn't leak out of the postfix into the vanilla LateUpdate caller.
        try
        {
            if (__instance != StartOfRound.Instance?.localPlayerController) return;
            if (BetterLethalVRMManager.Instance?.FreeLookTerminalLadder?.Value != true) return;

            // gameplayCamera may briefly be null during death/respawn transitions
            if (__instance.gameplayCamera == null) return;
            var cam = __instance.gameplayCamera.transform;
            bool restricted = __instance.inTerminalMenu || __instance.isClimbingLadder;

            if (!restricted)
            {
                if (_wasRestricted)
                {
                    if (cam.localPosition != _entryLocalPos) cam.localPosition = _entryLocalPos;
                    if (cam.localEulerAngles != _entryLocalEuler) cam.localEulerAngles = _entryLocalEuler;
                    _freeLookH = 0f;
                    _freeLookV = 0f;
                }
                _wasRestricted = false;
                return;
            }

            if (!_wasRestricted)
            {
                var e = cam.localEulerAngles;
                _entryLocalEuler = new Vector3(e.x > 180f ? e.x - 360f : e.x, e.y, e.z);
                _entryLocalPos = cam.localPosition;
                _freeLookH = 0f;
                _freeLookV = 0f;
            }
            _wasRestricted = true;

            float sens = BetterLethalVRMManager.Instance.FreeLookSensitivity.Value * 0.06f;
            var mouseDelta = Mouse.current?.delta.ReadValue() ?? Vector2.zero;
            _freeLookH += mouseDelta.x * sens;
            _freeLookV -= mouseDelta.y * sens;
            _freeLookV = Mathf.Clamp(_freeLookV, -80f, 80f);

            Vector3 targetEuler = new Vector3(_entryLocalEuler.x + _freeLookV, _entryLocalEuler.y + _freeLookH, _entryLocalEuler.z);

            if (cam.localPosition != _entryLocalPos) cam.localPosition = _entryLocalPos;
            if (cam.localEulerAngles != targetEuler) cam.localEulerAngles = targetEuler;
        }
        catch (Exception e)
        {
            Debug.LogError($"BetterLethalVRM free-look camera update failed: {e}");
        }
    }
}