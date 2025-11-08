using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Scoops.service
{
    public class Modifier : MonoBehaviour
    {
        public static Action toggle;
        public static bool intensifying = true;

        public virtual void Start()
        {
            toggle += ToggleIntensify;
            ToggleIntensify();
        }

        public virtual void ToggleIntensify()
        {
            // Do nothing
        }

        public void OnDestroy()
        {
            toggle -= ToggleIntensify;
        }
    }

    public class LightModifier : Modifier
    {
        private Light light;
        private float origIntensity;

        public override void Start()
        {
            light = this.GetComponent<Light>();
            origIntensity = light.intensity;

            base.Start();
        }

        public override void ToggleIntensify()
        {
            if (light == null)
            {
                GameObject.Destroy(this);
                return;
            }

            if (intensifying)
            {
                light.intensity = origIntensity * 1.05f;
            } 
            else
            {
                light.intensity = origIntensity;
            }
        }
    }

    public class FogModifier : Modifier
    {
        private LocalVolumetricFog fog;
        private float origDistance;

        public override void Start()
        {
            fog = this.GetComponent<LocalVolumetricFog>();
            origDistance = fog.parameters.meanFreePath;

            base.Start();
        }

        public override void ToggleIntensify()
        {
            if (fog == null)
            {
                GameObject.Destroy(this);
                return;
            }

            if (intensifying)
            {
                fog.parameters.meanFreePath = origDistance * 0.6f;
            }
            else
            {
                fog.parameters.meanFreePath = origDistance;
            }
        }
    }

    public static class LightService
    {
        public static string planetName;

        public static void UpdateAllLights()
        {
            if (Config.changeLightFadeDistance.Value)
            {
                HDAdditionalLightData[] allLightData = UnityEngine.Object.FindObjectsByType<HDAdditionalLightData>(FindObjectsInactive.Include, FindObjectsSortMode.None);

                float multiplier = Config.lightVolumetricDistMult.Value;
                float cap = Config.lightVolumetricDistCap.Value;

                foreach (HDAdditionalLightData lightData in allLightData)
                {
                    lightData.fadeDistance = Math.Clamp(multiplier * lightData.range, 5f, cap);
                }
            }

            if (Config.volumetricCompensation.Value && (Config.useCustomShader.Value && Config.useLegacyCustomShader.Value))
            {
                Light[] lights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (Light light in lights)
                {
                    if (!light.GetComponent<LightModifier>())
                    {
                        light.gameObject.AddComponent<LightModifier>();
                    }
                }

                LocalVolumetricFog[] fogs = UnityEngine.Object.FindObjectsByType<LocalVolumetricFog>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (LocalVolumetricFog fog in fogs)
                {
                    if (!fog.GetComponent<FogModifier>())
                    {
                        fog.gameObject.AddComponent<FogModifier>();
                    }
                }

                planetName = (new string(RoundManager.Instance.currentLevel.PlanetName.SkipWhile((char c) => !char.IsLetter(c)).ToArray())).Trim().ToLower();

                CheckCompensationStatus(GameNetworkManager.Instance.localPlayerController);
            }
        }

        public static void CheckCompensationStatus(PlayerControllerB player)
        {
            if (player != null && planetName != null && Config.compensationMoonBlacklist.Value.Split(';').Contains(planetName))
            {
                PlayerControllerB focusedPlayer = player;
                if (focusedPlayer.isPlayerDead && focusedPlayer.spectatedPlayerScript != null) focusedPlayer = focusedPlayer.spectatedPlayerScript;
                SetLightIntensity(focusedPlayer.isInsideFactory);
            }
            else
            {
                SetLightIntensity(true);
            }
        }

        public static void ToggleLightIntensity()
        {
            if (Modifier.toggle != null)
            {
                Modifier.intensifying = !Modifier.intensifying;
                Modifier.toggle.Invoke();
            }
        }

        public static void SetLightIntensity(bool enabled)
        {
            if (Modifier.toggle != null)
            {
                Plugin.Log.LogInfo("Setting light intensity - " + enabled);
                Modifier.intensifying = enabled;
                Modifier.toggle.Invoke();
            }
        }

        [HarmonyPatch(typeof(PlayerControllerB))]
        [HarmonyPatch("KillPlayer")]
        [HarmonyPostfix]
        public static void PlayerControllerB_KillPlayer(ref PlayerControllerB __instance)
        {
            if (Config.volumetricCompensation.Value && (Config.useCustomShader.Value && Config.useLegacyCustomShader.Value))
            {
                if ((!__instance.IsOwner || !__instance.isPlayerControlled || (__instance.IsServer && !__instance.isHostPlayerObject)) && !__instance.isTestingPlayer)
                {
                    return;
                }

                CheckCompensationStatus(__instance);
            }
        }

        [HarmonyPatch(typeof(PlayerControllerB))]
        [HarmonyPatch("SpectateNextPlayer")]
        [HarmonyPostfix]
        public static void PlayerControllerB_SpectateNextPlayer(ref PlayerControllerB __instance)
        {
            if (Config.volumetricCompensation.Value && (Config.useCustomShader.Value && Config.useLegacyCustomShader.Value))
            {
                if ((!__instance.IsOwner || !__instance.isPlayerControlled || (__instance.IsServer && !__instance.isHostPlayerObject)) && !__instance.isTestingPlayer)
                {
                    return;
                }

                CheckCompensationStatus(__instance);
            }
        }

        [HarmonyPatch(typeof(StartOfRound))]
        [HarmonyPatch("EndOfGameClientRpc")]
        [HarmonyPostfix]
        public static void StartOfRound_EndOfGameClientRpc(ref StartOfRound __instance)
        {
            if (Config.volumetricCompensation.Value && (Config.useCustomShader.Value && Config.useLegacyCustomShader.Value))
            {
                planetName = null;
                SetLightIntensity(true);
            }
        }
    }

    [HarmonyPatch]
    class TeleportPatches
    {
        static IEnumerable<MethodBase> TargetMethods() => new[]
        {
            AccessTools.Method(typeof(EntranceTeleport), "TeleportPlayer"),
            AccessTools.Method(typeof(ShipTeleporter), "beamUpPlayer"),
            AccessTools.Method(typeof(ShipTeleporter), "TeleportPlayerOutWithInverseTeleporter"),
            AccessTools.Method(typeof(StartOfRound), "ReviveDeadPlayers"),
        };

        static void Postfix()
        {
            if (Config.volumetricCompensation.Value && (Config.useCustomShader.Value && Config.useLegacyCustomShader.Value))
            {
                LightService.CheckCompensationStatus(GameNetworkManager.Instance.localPlayerController);
            }
        }
    }
}
