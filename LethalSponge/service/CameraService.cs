using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Scoops.service
{
    public static class CameraService
    {
        public static Camera MapCamera;
        public static Camera ShipCamera;
        public static Camera SecurityCamera;

        public static GameObject MonitorWall;

        public static bool Init()
        {
            Plugin.Log.LogMessage("Finding Ship cameras.");
            bool success = true;
            Camera mapCamera = GameObject.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(x => x.gameObject.name == "MapCamera" && x.gameObject.tag == "MapCamera").FirstOrDefault();
            if (mapCamera != default(Camera))
            {
                MapCamera = mapCamera;
            } 
            else
            {
                Plugin.Log.LogError("Sponge could not find MapCamera. Camera fixes may not function.");
                success = false;
            }

            Camera shipCamera = GameObject.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(x => x.gameObject.name == "ShipCamera" && x.gameObject.tag == "Untagged").FirstOrDefault();
            if (shipCamera != default(Camera))
            {
                ShipCamera = shipCamera;
            }
            else
            {
                Plugin.Log.LogError("Sponge could not find ShipCamera. Camera fixes may not function.");
                success = false;
            }

            Camera securityCamera = GameObject.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(x => x.gameObject.name == "SecurityCamera" && x.gameObject.tag == "Untagged").FirstOrDefault();
            if (securityCamera != default(Camera))
            {
                SecurityCamera = securityCamera;
            }
            else
            {
                Plugin.Log.LogError("Sponge could not find SecurityCamera. Camera fixes may not function.");
                success = false;
            }

            GameObject monitorWall = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(x => x.name == "MonitorWall" && x.gameObject.tag == "Untagged").FirstOrDefault();
            if (monitorWall != default(Camera))
            {
                MonitorWall = monitorWall;
            }
            else
            {
                Plugin.Log.LogError("Sponge could not find MonitorWall. Camera fixes may not function.");
                success = false;
            }

            return success;
        }

        public static void ApplyCameraFixes()
        {
            SetOverrides(ShipCamera);
            SetOverrides(SecurityCamera);
            SetOverrides(MapCamera, true);

            ShipCamera.farClipPlane = 13f;
            SecurityCamera.farClipPlane = 13f;

            ShipCamera.GetComponent<ManualCameraRenderer>().renderAtLowerFramerate = true;
            ShipCamera.GetComponent<ManualCameraRenderer>().fps = 15;

            SecurityCamera.GetComponent<ManualCameraRenderer>().renderAtLowerFramerate = true;
            SecurityCamera.GetComponent<ManualCameraRenderer>().fps = 15;

            MonitorWall.transform.Find("Cube.001/CameraMonitorScript").GetComponent<ManualCameraRenderer>().renderAtLowerFramerate = true;
            MonitorWall.transform.Find("Cube.001/CameraMonitorScript").GetComponent<ManualCameraRenderer>().fps = 15;

            Plugin.Log.LogMessage("Ship cameras patched.");
        }

        private static void SetOverrides(Camera camera, bool mapCamera = false)
        {
            HDAdditionalCameraData hdCameraData = camera.GetComponent<HDAdditionalCameraData>();
            hdCameraData.customRenderingSettings = true;
            hdCameraData.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)FrameSettingsField.MSAAMode] = true;
            hdCameraData.renderingPathCustomFrameSettings.msaaMode = MSAAMode.None;
            hdCameraData.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)FrameSettingsField.MaximumLODLevelMode] = true;
            hdCameraData.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)FrameSettingsField.MaximumLODLevel] = true;
            hdCameraData.renderingPathCustomFrameSettings.maximumLODLevelMode = MaximumLODLevelMode.OverrideQualitySettings;
            hdCameraData.renderingPathCustomFrameSettings.maximumLODLevel = 1;
            hdCameraData.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)FrameSettingsField.MaterialQualityLevel] = true;
            hdCameraData.renderingPathCustomFrameSettings.materialQuality = UnityEngine.Rendering.MaterialQuality.Low;
            if (!mapCamera) hdCameraData.DisableHDField(FrameSettingsField.TransparentObjects);
            hdCameraData.DisableHDField(FrameSettingsField.Decals);
            hdCameraData.DisableHDField(FrameSettingsField.TransparentPrepass);
            hdCameraData.DisableHDField(FrameSettingsField.TransparentPostpass);
            hdCameraData.DisableHDField(FrameSettingsField.RayTracing);
            hdCameraData.DisableHDField(FrameSettingsField.CustomPass);
            hdCameraData.DisableHDField(FrameSettingsField.MotionVectors);
            hdCameraData.DisableHDField(FrameSettingsField.Refraction);
            hdCameraData.DisableHDField(FrameSettingsField.Distortion);
            hdCameraData.DisableHDField(FrameSettingsField.Postprocess);
            hdCameraData.DisableHDField(FrameSettingsField.AfterPostprocess);
            hdCameraData.DisableHDField(FrameSettingsField.VirtualTexturing);
            hdCameraData.DisableHDField(FrameSettingsField.Water);
            hdCameraData.DisableHDField(FrameSettingsField.ShadowMaps);
            hdCameraData.DisableHDField(FrameSettingsField.ContactShadows);
            hdCameraData.DisableHDField(FrameSettingsField.ProbeVolume);
            hdCameraData.DisableHDField(FrameSettingsField.ScreenSpaceShadows);
            hdCameraData.DisableHDField(FrameSettingsField.SSR);
            hdCameraData.DisableHDField(FrameSettingsField.SSGI);
            hdCameraData.DisableHDField(FrameSettingsField.SSAO);
            hdCameraData.DisableHDField(FrameSettingsField.Transmission);
            hdCameraData.DisableHDField(FrameSettingsField.AtmosphericScattering);
            hdCameraData.DisableHDField(FrameSettingsField.ReflectionProbe);
            hdCameraData.DisableHDField(FrameSettingsField.PlanarProbe);
            hdCameraData.DisableHDField(FrameSettingsField.SkyReflection);
            hdCameraData.DisableHDField(FrameSettingsField.SubsurfaceScattering);
            hdCameraData.DisableHDField(FrameSettingsField.VolumetricClouds);
            if (mapCamera) hdCameraData.DisableHDField(FrameSettingsField.DirectSpecularLighting);
        }

        private static void DisableHDField(this HDAdditionalCameraData data, FrameSettingsField field)
        {
            data.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)field] = true;
            data.renderingPathCustomFrameSettings.SetEnabled(field, false);
        }
    }
}
