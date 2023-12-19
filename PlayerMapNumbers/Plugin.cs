using BepInEx;
using UnityEngine;
using TMPro;
using GameNetcodeStuff;
using HarmonyLib;
using System.Collections.Generic;
using System;
using Unity.Netcode;
using System.Text.RegularExpressions;
using System.Reflection;
using BepInEx.Logging;
using System.Text;

namespace PlayerMapNumbers
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource StaticLogger;
        private void Awake()
        {
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            StaticLogger = Logger;
            Harmony HarmonyInstance = new Harmony(PluginInfo.PLUGIN_GUID);
            Logger.LogInfo($"Attempting to patch with Harmony!");
            try
            {
                HarmonyInstance.PatchAll();
                Logger.LogInfo($"Patching success!");
            }
            catch (Exception ex)
            {
                StaticLogger.LogError("Failed to patch: " + ex?.ToString());
            }


        }
        public static void AddTargetNumber(UnityEngine.GameObject target, int number)
        {
            StaticLogger.LogInfo($"Adding index {number}");
            // Alive players
            GameObject parent = null;// target.transform.Find("Misc")?.Find("MapDot")?.gameObject;
            var script = target.GetComponent<PlayerControllerB>();

            if (script!=null&& script.isPlayerDead )
            {
                StaticLogger.LogInfo("Dead");
                if ( script.deadBody != null )
                {
                    StaticLogger.LogInfo("Has body");
                    parent = script.deadBody.transform.Find("MapDot")?.gameObject;
                }
            }
            else
            {
                StaticLogger.LogInfo("Not Dead");
                parent = target.transform.Find("Misc")?.Find("MapDot")?.gameObject;
            }

            // Radar boosters
            if (parent == null)
            {
                StaticLogger.LogInfo("Maybe Radar Booster");
                parent = target.transform.Find("RadarBoosterDot")?.gameObject;
            }

            if (parent == null)
            {
                StaticLogger.LogWarning("No parent findable");
                return;
            }

            GameObject labelObject = parent.transform.Find("TargetNumberLabel")?.gameObject;
            TextMeshPro textRef;
            if (labelObject == null)
            {
                labelObject = new GameObject();
                labelObject.transform.SetParent(parent.transform, false);
                labelObject.transform.SetLocalPositionAndRotation(new Vector3(0, 0.5f, 0), Quaternion.Euler(new Vector3(90, 0, 0)));
                labelObject.transform.localScale = Vector3.one / 2.0f;
                labelObject.layer = parent.layer;
                labelObject.name = "TargetNumberLabel";
                labelObject.AddComponent<KeepNorth>();
                textRef = labelObject.AddComponent<TextMeshPro>();
                textRef.alignment = TextAlignmentOptions.Center;
                textRef.autoSizeTextContainer = true;
                textRef.maxVisibleLines = 1;
                textRef.maxVisibleWords = 1;
            }
            else
            {
                textRef = labelObject.transform.GetComponent<TextMeshPro>();
            }
            textRef.text = ( 1 + number ).ToString();
        }

        static public void UpdateNumbers()
        {
            if (StartOfRound.Instance?.mapScreen == null)
            {
                return;
            }
            for (int index = 0; index < StartOfRound.Instance.mapScreen.radarTargets.Count; ++index)
            {
                var transAndName = StartOfRound.Instance.mapScreen.radarTargets[index];
                if (transAndName.transform != null)
                {
                    StaticLogger.LogInfo($"Name: {transAndName.name} index: {index} isNonPlayer: {transAndName.isNonPlayer}");
                    AddTargetNumber(transAndName.transform.gameObject, index);
                }
            }
        }

    }

    public class KeepNorth : MonoBehaviour
    {
        public void Awake()
        {
        }

        public void Update()
        {
            // Lock to north, which is actully not on the expected vector
            gameObject.transform.rotation = Quaternion.Euler(90, -45, 0);
        }
    }

    [HarmonyPatch(typeof(ManualCameraRenderer), "Awake")]
    public static class ManualCameraRendererAwakePatch
    {
        public static void Postfix(ManualCameraRenderer __instance)
        {
            Plugin.StaticLogger.LogInfo("ManualCameraRendererAwakePatch patch run");
            NetworkManager networkManager = __instance.NetworkManager;
            if ((UnityEngine.Object)networkManager == (UnityEngine.Object)null || !networkManager.IsListening)
                return;
            Plugin.UpdateNumbers();
        }
    }

    [HarmonyPatch(typeof(ManualCameraRenderer), "RemoveTargetFromRadar")]
    public static class ManualCameraRendererRemoveTargetFromRadarPatch
    {
        public static void Postfix(ManualCameraRenderer __instance, Transform removeTransform)
        {
            Plugin.StaticLogger.LogInfo("ManualCameraRendererRemoveTargetFromRadarPatch patch run");
            Plugin.UpdateNumbers();
        }
    }

    [HarmonyPatch(typeof(ManualCameraRenderer), "AddTransformAsTargetToRadar")]
    public static class ManualCameraRendererAddTransformAsTargetToRadarPatch
    {
        public static void Postfix(ManualCameraRenderer __instance, Transform newTargetTransform, string targetName, bool isNonPlayer)
        {
            Plugin.StaticLogger.LogInfo("ManualCameraRendererAddTransformAsTargetToRadarPatch patch run");
            Plugin.UpdateNumbers();
        }
    }

    [HarmonyPatch(typeof(PlayerControllerB), "SendNewPlayerValuesClientRpc")]
    public static class SendNewPlayerValuesClientRpcPatch
    {
        public static void Postfix(PlayerControllerB __instance, ref ulong[] playerSteamIds)
        {
            Plugin.StaticLogger.LogInfo("SendNewPlayerValuesClientRpcPatch patch run");
            Plugin.UpdateNumbers();
        }
    }

    [HarmonyPatch(typeof(PlayerControllerB), "SendNewPlayerValuesServerRpc")]
    public static class SendNewPlayerValuesServerRpcPatch
    {
        public static void Postfix(PlayerControllerB __instance, ulong newPlayerSteamId)
        {
            Plugin.StaticLogger.LogInfo("SendNewPlayerValuesServerRpcPatch patch run");
            Plugin.UpdateNumbers();
        }
    }

    [HarmonyPatch(typeof(PlayerControllerB), "SpawnDeadBody")]
    public static class SpawnDeadBodyPatch
    {
        public static void Postfix(PlayerControllerB __instance)
        {
            Plugin.StaticLogger.LogInfo("SpawnDeadBodyPatch patch run");
            Plugin.UpdateNumbers();
        }
    }

    [HarmonyPatch(typeof(PlayerControllerB), "KillPlayerServerRpc")]
    public static class KillPlayerServerRpcPatch
    {
        public static void Postfix(PlayerControllerB __instance)
        {
            Plugin.StaticLogger.LogInfo("KillPlayerServerRpcPatch patch run");
            Plugin.UpdateNumbers();
        }
    }

    [HarmonyPatch(typeof(PlayerControllerB), "KillPlayerClientRpc")]
    public static class KillPlayerClientRpcPatch
    {
        public static void Postfix(PlayerControllerB __instance)
        {
            Plugin.StaticLogger.LogInfo("KillPlayerClientRpcPatch patch run");
            Plugin.UpdateNumbers();
        }
    }

    [HarmonyPatch(typeof(Terminal), "ParsePlayerSentence")]
    public static class TerminalParsePlayerSentencePatch
    {
        static private string RemovePunctuation(string s)
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (char c in s)
            {
                if (!char.IsPunctuation(c))
                    stringBuilder.Append(c);
            }
            return stringBuilder.ToString().ToLower();
        }
        public static void Postfix(Terminal __instance, ref TerminalNode __result)
        {
            Plugin.StaticLogger.LogInfo("TerminalParsePlayerSentence patch run");
            // 10, 11, 12 parse errors
            if (__result == __instance.terminalNodes.specialNodes[10]) {
                Plugin.StaticLogger.LogInfo("Extended Parse");
                string str1 = RemovePunctuation(__instance.screenText.text.Substring(__instance.screenText.text.Length - __instance.textAdded));
                string[] strArray = str1.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                int outputNum;
                if ( strArray.Length == 1 && int.TryParse( strArray[0], out outputNum ) )
                {
                    Plugin.StaticLogger.LogInfo("Number Found");
                    int playerIndex = outputNum - 1;
                    if ( playerIndex < StartOfRound.Instance.mapScreen.radarTargets.Count )
                    {
                        Plugin.StaticLogger.LogInfo("Valid Number");
                        var controller = StartOfRound.Instance.mapScreen.radarTargets[playerIndex].transform.gameObject.GetComponent<PlayerControllerB>();
                        if ( controller != null &&
                             !controller.isPlayerControlled && !controller.isPlayerDead && controller.redirectToEnemy == null )
                        {
                            return;
                        }
                        StartOfRound.Instance.mapScreen.SwitchRadarTargetAndSync(playerIndex);
                        Plugin.StaticLogger.LogInfo("Updated Target");
                        __result = __instance.terminalNodes.specialNodes[20];
                    }
                }
            }
        }
    }

}