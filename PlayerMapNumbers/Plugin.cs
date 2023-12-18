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
        public static void AddPlayerNumber(UnityEngine.GameObject player, int number)
        {
            Plugin.StaticLogger.LogInfo($"Adding index {number}");
            var parent = player.transform.Find("Misc").Find("MapDot").gameObject;
            GameObject labelObject = parent.transform.Find("PlayerNumberLabel")?.gameObject;
            TextMeshPro textRef;
            if (labelObject == null)
            {
                labelObject = new GameObject();
                labelObject.transform.SetParent(parent.transform, false);
                labelObject.transform.SetLocalPositionAndRotation(new Vector3(0, 0.5f, 0), Quaternion.Euler(new Vector3(90, 0, 0)));
                labelObject.transform.localScale = Vector3.one / 2.0f;
                labelObject.layer = parent.layer;
                labelObject.name = "PlayerNumberLabel";
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

    }

    [HarmonyPatch(typeof(ManualCameraRenderer), "Awake")]
    public static class ManualCameraRendererPatch
    {
        public static void Postfix(ManualCameraRenderer __instance)
        {
            Plugin.StaticLogger.LogInfo("Postfix patch run");
            NetworkManager networkManager = __instance.NetworkManager;
            if ((UnityEngine.Object)networkManager == (UnityEngine.Object)null || !networkManager.IsListening)
                return;
            if ( StartOfRound.Instance?.mapScreen == null )
            {
                return;
            }
            for (int index = 0; index < StartOfRound.Instance.allPlayerScripts.Length; ++index)
            {      
                if ( index > StartOfRound.Instance.mapScreen.radarTargets.Count )
                {
                    break;
                }
                var transAndName = StartOfRound.Instance.mapScreen.radarTargets[index];
                if (transAndName.transform != null) {
                    Plugin.AddPlayerNumber(transAndName.transform.gameObject, index);
                }
            }
        }
    }
}