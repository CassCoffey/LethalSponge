using GameNetcodeStuff;
using HarmonyLib;
using Scoops.service;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.InputSystem;
using UnityEngineInternal.Input;

namespace Scoops.patches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    public class PlayerControllerBSpongePatch
    {
        [HarmonyPatch("ConnectClientToPlayerObject")]
        [HarmonyPostfix]
        private static void PlayerControllerB_ConnectClientToPlayerObject(ref PlayerControllerB __instance)
        {
            CameraService.ApplyPlayerCameraPatch(__instance);
        }
    }
}
