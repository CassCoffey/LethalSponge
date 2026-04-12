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
                    lightData.fadeDistance = Mathf.Clamp(multiplier * lightData.range, 5f, Mathf.Min(cap, lightData.fadeDistance));
                    lightData.shadowFadeDistance = Mathf.Clamp(multiplier * lightData.range, 5f, Mathf.Min(cap, lightData.shadowFadeDistance));
                }
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
    }
}
