using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using GameNetcodeStuff;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace OomJan.BetterLethalVRM;

[HarmonyPatch]
public static class MessagePatch {
    private const char SPERATOR = ';';
    
    private const string FACE_PREFIX = "[betterlethalvrm.face]";

    private static MethodInfo addTextMessageServerRpc =
        AccessTools.Method(typeof(HUDManager), "AddTextMessageServerRpc");

    private static string BuildFaceMessage(ulong targetId, ulong ownerId, int blinkIdx, int mouthIdx, float min, float max, float dur) =>
        string.Join(SPERATOR.ToString(),
            FACE_PREFIX, targetId, ownerId, blinkIdx, mouthIdx,
            min.ToString(CultureInfo.InvariantCulture),
            max.ToString(CultureInfo.InvariantCulture),
            dur.ToString(CultureInfo.InvariantCulture));

    private static void SendFaceSettings(ulong targetClientId, ulong ownerClientId)
    {
        var mgr = BetterLethalVRMManager.Instance;
        addTextMessageServerRpc?.Invoke(HUDManager.Instance, [BuildFaceMessage(
            targetClientId, ownerClientId,
            mgr.BlinkBlendshapeIndex.Value, mgr.MouthBlendshapeIndex.Value,
            mgr.BlinkIntervalMin.Value, mgr.BlinkIntervalMax.Value, mgr.BlinkDuration.Value
        )]);
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerControllerB), "ConnectClientToPlayerObject")]
    public static void ConnectClientToPlayerObject_postfix(PlayerControllerB __instance) {
        // Wrap the whole postfix: this hooks the connection flow, so a transient null in
        // HUDManager.Instance or our own Instance during init must not escape and disrupt
        // the player join sequence. Worst case we just skip the broadcast.
        try {
            BetterLethalVRMManager.Instance.PlayersScale.Clear();
            BetterLethalVRMManager.Instance.PlayersFaceSettings.Clear();

            addTextMessageServerRpc?.Invoke(HUDManager.Instance, [ $"[betterlethalvrm.size];{ulong.MaxValue};{__instance.playerClientId};{BetterLethalVRMManager.Instance.ScaleSize.Value}" ]);
            SendFaceSettings(ulong.MaxValue, __instance.playerClientId);
        }
        catch (Exception e) {
            Debug.LogError($"BetterLethalVRM connect postfix failed: {e}");
        }
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(HUDManager), "AddTextMessageServerRpc")]
    public static void AddTextMessagesServerRpc_postfix(HUDManager __instance, string chatMessage) {
        // Wrap the whole postfix: every chat message in the game runs through here, and a single
        // malformed/early/short payload would otherwise let an exception escape and break the chat
        // RPC for everyone in the lobby.
        try {
            NetworkManager networkManager = __instance.NetworkManager;
            if (networkManager == null || !networkManager.IsListening) return;

            if (chatMessage.StartsWith("[betterlethalvrm.size]")) {
                Debug.Log($"BetterLethalVRM: server get message: {chatMessage}");

                if (networkManager.IsHost) {
                    string[] messages = chatMessage.Split(SPERATOR);
                    ulong clientId = ulong.Parse(messages[1]);
                    ulong scaleClientId = ulong.Parse(messages[2]);
                    if (clientId == ulong.MaxValue) {
                        foreach (KeyValuePair<ulong, float> playerScale in BetterLethalVRMManager.Instance.PlayersScale.ToList()) {
                            if (playerScale.Key == scaleClientId) continue;
                            addTextMessageServerRpc?.Invoke(__instance,
                                [$"[betterlethalvrm.size];{scaleClientId};{playerScale.Key};{playerScale.Value}"]);
                        }
                    }
                }
            }
            else if (chatMessage.StartsWith(FACE_PREFIX)) {
                Debug.Log($"BetterLethalVRM: server get message: {chatMessage}");

                if (networkManager.IsHost) {
                    string[] messages = chatMessage.Split(SPERATOR);
                    ulong clientId = ulong.Parse(messages[1]);
                    ulong faceClientId = ulong.Parse(messages[2]);
                    if (clientId == ulong.MaxValue) {
                        foreach (KeyValuePair<ulong, BetterLethalVRMManager.FaceSettings> entry in BetterLethalVRMManager.Instance.PlayersFaceSettings.ToList()) {
                            if (entry.Key == faceClientId) continue;
                            var fs = entry.Value;
                            addTextMessageServerRpc?.Invoke(__instance, [BuildFaceMessage(
                                faceClientId, entry.Key,
                                fs.BlinkBlendshapeIndex, fs.MouthBlendshapeIndex,
                                fs.BlinkIntervalMin, fs.BlinkIntervalMax, fs.BlinkDuration
                            )]);
                        }
                    }
                }
            }
        }
        catch (Exception e) {
            Debug.LogError($"BetterLethalVRM server message handler failed: {e}");
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(HUDManager), "AddTextMessageClientRpc")]
    public static void AddTextMessageClientRpc_postfix(HUDManager __instance, string chatMessage) {
        // Wrap the whole postfix: same reasoning as the ServerRpc handler — protect every
        // incoming chat message from a malformed payload or transient null in StartOfRound.
        try {
            NetworkManager networkManager = __instance.NetworkManager;
            if (networkManager == null || !networkManager.IsListening) return;

            if (chatMessage.StartsWith("[betterlethalvrm.size]")) {
                Debug.Log($"BetterLethalVRM: client get message: {chatMessage}");

                if (networkManager.IsClient || networkManager.IsHost) {
                    string[] messages = chatMessage.Split(SPERATOR);
                    ulong clientId = ulong.Parse(messages[1]);
                    ulong scaleClientId = ulong.Parse(messages[2]);
                    float scaleSize = float.Parse(messages[3]);
                    if (clientId == ulong.MaxValue || clientId == Convert.ToUInt64(StartOfRound.Instance.thisClientPlayerId)) {
                        BetterLethalVRMManager.Instance.PlayersScale[scaleClientId] = scaleSize;
                        StartOfRound.Instance.allPlayerObjects[scaleClientId].transform.localScale =
                            new Vector3(scaleSize, scaleSize, scaleSize);
                    }
                }
            }
            else if (chatMessage.StartsWith(FACE_PREFIX)) {
                Debug.Log($"BetterLethalVRM: client get message: {chatMessage}");

                if (networkManager.IsClient || networkManager.IsHost) {
                    string[] messages = chatMessage.Split(SPERATOR);
                    ulong clientId = ulong.Parse(messages[1]);
                    ulong faceClientId = ulong.Parse(messages[2]);
                    if (clientId == ulong.MaxValue || clientId == Convert.ToUInt64(StartOfRound.Instance.thisClientPlayerId)) {
                        var faceSettings = new BetterLethalVRMManager.FaceSettings
                        {
                            BlinkBlendshapeIndex = int.Parse(messages[3]),
                            MouthBlendshapeIndex = int.Parse(messages[4]),
                            BlinkIntervalMin = float.Parse(messages[5], CultureInfo.InvariantCulture),
                            BlinkIntervalMax = float.Parse(messages[6], CultureInfo.InvariantCulture),
                            BlinkDuration = float.Parse(messages[7], CultureInfo.InvariantCulture)
                        };
                        BetterLethalVRMManager.Instance.PlayersFaceSettings[faceClientId] = faceSettings;

                        // 若 instance 已載入，直接更新
                        foreach (var instance in BetterLethalVRMManager.Instance.GetInstances())
                        {
                            if (instance.PlayerControllerB.playerClientId == faceClientId)
                            {
                                instance.BlinkBlendshapeIndex = faceSettings.BlinkBlendshapeIndex;
                                instance.MouthBlendshapeIndex = faceSettings.MouthBlendshapeIndex;
                                instance.BlinkIntervalMin = faceSettings.BlinkIntervalMin;
                                instance.BlinkIntervalMax = faceSettings.BlinkIntervalMax;
                                instance.BlinkDuration = faceSettings.BlinkDuration;
                                break;
                            }
                        }
                    }
                }
            }
        }
        catch (Exception e) {
            Debug.LogError($"BetterLethalVRM client message handler failed: {e}");
        }
    }
}