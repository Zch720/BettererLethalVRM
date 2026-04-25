using System;
using System.Collections.Generic;
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
    
    private static MethodInfo addTextMessageServerRpc =
        AccessTools.Method(typeof(HUDManager), "AddTextMessageServerRpc");
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerControllerB), "ConnectClientToPlayerObject")]
    public static void ConnectClientToPlayerObject_postfix(PlayerControllerB __instance) {
        BetterLethalVRMManager.Instance.PlayersScale.Clear();
        
        addTextMessageServerRpc?.Invoke(HUDManager.Instance, [ $"[betterlethalvrm.size];{ulong.MaxValue};{__instance.playerClientId};{BetterLethalVRMManager.Instance.ScaleSize.Value}" ]);
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(HUDManager), "AddTextMessageServerRpc")]
    public static void AddTextMessagesServerRpc_postfix(HUDManager __instance, string chatMessage) {
        if (chatMessage.StartsWith("[betterlethalvrm.size]")) {
            NetworkManager networkManager = __instance.NetworkManager;
            if (networkManager == null || !networkManager.IsListening) return;
            
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
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(HUDManager), "AddTextMessageClientRpc")]
    public static void AddTextMessageClientRpc_postfix(HUDManager __instance, string chatMessage) {
        if (chatMessage.StartsWith("[betterlethalvrm.size]")) {
            NetworkManager networkManager = __instance.NetworkManager;
            if (networkManager == null || !networkManager.IsListening) return;
            
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
    }
}