using HarmonyLib;
using Scoops.service;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Scoops.patches
{
    public class MainMenuSpongePatch
    {
        [HarmonyPatch(typeof(MenuManager), "Start")]
        [HarmonyPostfix]
        public static void Start(ref MenuManager __instance)
        {
            if (__instance.isInitScene) return;

            Plugin.Log.LogMessage("Calling initial Resources.UnloadUnusedAssets().");
            Resources.UnloadUnusedAssets();
        }
    }
}
