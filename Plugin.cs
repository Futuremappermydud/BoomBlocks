using BepInEx;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace BoomBlocks
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Mod : BaseUnityPlugin
    {
        public const string PluginAuthor = "FutureMapper";
        public const string PluginGuid = "com.github.FutureMapper.BoomBlocks";
        public const string PluginName = "Boom Blocks";
        public const string PluginVersion = "1.1.0";

        public static Mod instance;

        private static readonly Harmony harmony = new Harmony(PluginGuid);
        public static GameObject Block;
        public static bool startedLoad = false;

        private void Awake()
        {
            instance = this;
            harmony.PatchAll();
        }

        private void OnDestroy()
        {
            harmony.UnpatchSelf();
        }

        [HarmonyPatch(typeof(Menu), "Start")]
        private class LoadPatch
        {
            private static void Postfix()
            {
                if (startedLoad) return;
                AssetBundle bund = AssetBundle.LoadFromFile(Path.Combine(Application.dataPath, "note.drum"));
                startedLoad = true;
                foreach (string asset in bund.GetAllAssetNames())
                {
                    instance.Logger.LogInfo(asset);
                }
                Block = bund.LoadAsset<GameObject>("assets/note.prefab");
            }
        }

        [HarmonyPatch(typeof(BoomboxHitObject), "HandleSpawning")]
        private class CreateObjectPatch
        {
            public static bool disablePulse = false;
            private static void Postfix(BoomboxHitObject __instance, ref Vector3 ____initialScale, ref MeshRenderer ____hitObjectRenderer, ref Color ____beatColorRight, ref Color ____beatColorLeft, ref Color ____initialColorRight, ref Color ____initialColorLeft, ref SGameplayMetrics ____metrics)
            {
                if (!__instance.gameObject.name.Contains("(Clone)")) return;
                if (!Block || __instance.transform.Find("Note(Clone)") != null) return;
                __instance.transform.Find("SM_DrumsBackFace").gameObject.SetActive(false);
                Instantiate(Block).transform.SetParent(__instance.transform, false);
                ____initialScale = Vector3.one;
                if(disablePulse)
                {
                    ____beatColorLeft = ____initialColorLeft;
                    ____beatColorRight = ____initialColorRight;
                }
            }
        }

        [HarmonyPatch(typeof(BoomboxHitObject))]
        [HarmonyPatch("HandleBeatColor")]
        public static class ColorPatch
        {
            private static void Postfix(BoomboxHitObject __instance, Color ____currentColor)
            {
                Transform note = __instance.transform.Find("Note(Clone)");
                if(!note) return;
                note.GetComponent<MeshRenderer>().material.SetColor("_EmissionColor", ____currentColor);
            }
        }
    }
}   