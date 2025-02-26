using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Scoops.service
{
    public static class LightService
    {
        public static void UpdateAllLights()
        {
            HDAdditionalLightData[] allLightData = UnityEngine.Object.FindObjectsByType<HDAdditionalLightData>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            float multiplier = Config.lightVolumetricDistMult.Value;
            float cap = Config.lightVolumetricDistCap.Value;

            foreach (HDAdditionalLightData lightData in allLightData)
            {
                lightData.fadeDistance = Math.Clamp(multiplier * lightData.range, 0f, cap);
            }
        }
    }
}
