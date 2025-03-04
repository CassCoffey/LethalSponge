using System;
using System.Collections.Generic;
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
        public static void UpdateAllLights()
        {
            if (Config.changeLightFadeDistance.Value)
            {
                HDAdditionalLightData[] allLightData = UnityEngine.Object.FindObjectsByType<HDAdditionalLightData>(FindObjectsInactive.Include, FindObjectsSortMode.None);

                float multiplier = Config.lightVolumetricDistMult.Value;
                float cap = Config.lightVolumetricDistCap.Value;

                foreach (HDAdditionalLightData lightData in allLightData)
                {
                    lightData.fadeDistance = Math.Clamp(multiplier * lightData.range, 0f, cap);
                }
            }

            if (Config.volumetricCompensation.Value && (Config.useCustomShader.Value || Config.useWIPCustomShader.Value))
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
    }
}
