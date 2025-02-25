using GameNetcodeStuff;
using Scoops.rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using static UnityEngine.GraphicsBuffer;

namespace Scoops.service
{
    public static class CameraService
    {
        public static Camera MapCamera;
        public static Camera ShipCamera;
        public static Camera SecurityCamera;

        public static GameObject MonitorWall;
        public static GameObject ShipInside;

        public static Terminal MainTerminal;

        public static GameObject oldVolume;
        public static GameObject newVolume;
        public static GameObject volumetricMainVolume;
        public static GameObject volumetricSubVolume;
        public static GameObject volumetricFinalVolume;

        public static RTHandle volumetricOverlay;
        public static RTHandle shadowMapAtlas;

        public static MainCamDepthCopy mainCamDepthPass;
        public static VolumetricCamDepthWrite volumetricCamDepthPass;
        public static VolumetricCamOverlay volumetricCamOverlayPass;

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
            if (monitorWall != default(GameObject))
            {
                MonitorWall = monitorWall;
            }
            else
            {
                Plugin.Log.LogError("Sponge could not find MonitorWall. Camera fixes may not function.");
                success = false;
            }

            Terminal mainTerminal = GameObject.FindObjectsByType<Terminal>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(x => x.gameObject.name == "TerminalScript" && x.gameObject.tag == "InteractTrigger").FirstOrDefault();
            if (mainTerminal != default(Terminal))
            {
                MainTerminal = mainTerminal;
            }

            return success;
        }

        public static void DisablePosterization()
        {
            oldVolume = GameObject.Find("CustomPass");
            oldVolume.SetActive(false);

            if (Config.useCustomShader.Value)
            {
                volumetricOverlay = RTHandles.Alloc(860, 520, depthBufferBits: DepthBits.None, colorFormat: GraphicsFormat.R8G8B8A8_SRGB, dimension: TextureDimension.Tex2D, name: "VolumetricOverlay");
                shadowMapAtlas = RTHandles.Alloc(4096, 4096, depthBufferBits: DepthBits.Depth32, dimension: TextureDimension.Tex2D, name: "Shadow_Map_Atlas_Copy");

                // The old switcharoo
                newVolume = GameObject.Instantiate((GameObject)Plugin.SpongeAssets.LoadAsset("SpongeCustomPass"), oldVolume.transform.parent);

                volumetricMainVolume = newVolume.transform.Find("CustomVolumetricPassMain").gameObject;
                volumetricSubVolume = newVolume.transform.Find("CustomVolumetricPassSub").gameObject;
                volumetricFinalVolume = newVolume.transform.Find("CustomVolumetricPassFinal").gameObject;

                newVolume.GetComponent<CustomPassVolume>().injectionPoint = (CustomPassInjectionPoint)7;
            }
        }

        public static void ToggleVolumes()
        {
            oldVolume.SetActive(!oldVolume.activeSelf);
            if (newVolume)
            {
                newVolume.SetActive(!newVolume.activeSelf);
            }
        }

        public static void ApplyCameraFixes()
        {
            SetOverrides(ShipCamera);
            SetOverrides(SecurityCamera);
            SetOverrides(MapCamera, true);

            ShipCamera.farClipPlane = 13f;
            SecurityCamera.farClipPlane = 20f;

            ShipCamera.GetComponent<ManualCameraRenderer>().renderAtLowerFramerate = true;
            ShipCamera.GetComponent<ManualCameraRenderer>().fps = Config.securityCameraFramerate.Value;

            SecurityCamera.GetComponent<ManualCameraRenderer>().renderAtLowerFramerate = true;
            SecurityCamera.GetComponent<ManualCameraRenderer>().fps = Config.securityCameraFramerate.Value;

            MonitorWall.transform.Find("Cube.001/CameraMonitorScript").GetComponent<ManualCameraRenderer>().renderAtLowerFramerate = true;
            MonitorWall.transform.Find("Cube.001/CameraMonitorScript").GetComponent<ManualCameraRenderer>().fps = Config.mapCameraFramerate.Value;

            Plugin.Log.LogMessage("Ship cameras patched.");
        }

        public static void ApplyPlayerCameraPatch(PlayerControllerB player)
        {
            SetPlayerOverrides(player.gameplayCamera, Config.potatoCompany.Value);

            if (Config.useCustomShader.Value)
            {
                newVolume.GetComponent<CustomPassVolume>().targetCamera = player.gameplayCamera;
            }
        }

        private static void SetOverrides(Camera camera, bool mapCamera = false)
        {
            HDAdditionalCameraData hdCameraData = camera.GetComponent<HDAdditionalCameraData>();
            hdCameraData.customRenderingSettings = true;
            hdCameraData.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)FrameSettingsField.MSAAMode] = true;
            hdCameraData.renderingPathCustomFrameSettings.msaaMode = MSAAMode.None;
            if (!mapCamera)
            {
                hdCameraData.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)FrameSettingsField.MaximumLODLevelMode] = true;
                hdCameraData.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)FrameSettingsField.MaximumLODLevel] = true;
                hdCameraData.renderingPathCustomFrameSettings.maximumLODLevelMode = MaximumLODLevelMode.OverrideQualitySettings;
                hdCameraData.renderingPathCustomFrameSettings.maximumLODLevel = 2;
            }
            hdCameraData.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)FrameSettingsField.MaterialQualityLevel] = true;
            hdCameraData.renderingPathCustomFrameSettings.materialQuality = MaterialQuality.Low;
            if (!mapCamera) hdCameraData.DisableHDField(FrameSettingsField.TransparentObjects);
            hdCameraData.DisableHDField(FrameSettingsField.Decals);
            hdCameraData.DisableHDField(FrameSettingsField.TransparentPrepass);
            hdCameraData.DisableHDField(FrameSettingsField.TransparentPostpass);
            hdCameraData.DisableHDField(FrameSettingsField.RayTracing);
            hdCameraData.DisableHDField(FrameSettingsField.CustomPass);
            hdCameraData.DisableHDField(FrameSettingsField.MotionVectors);
            hdCameraData.DisableHDField(FrameSettingsField.Refraction);
            hdCameraData.DisableHDField(FrameSettingsField.Distortion);
            hdCameraData.DisableHDField(FrameSettingsField.CustomPostProcess);
            hdCameraData.DisableHDField(FrameSettingsField.StopNaN);
            hdCameraData.DisableHDField(FrameSettingsField.DepthOfField);
            hdCameraData.DisableHDField(FrameSettingsField.MotionBlur);
            hdCameraData.DisableHDField(FrameSettingsField.PaniniProjection);
            hdCameraData.DisableHDField(FrameSettingsField.Bloom);
            hdCameraData.DisableHDField(FrameSettingsField.LensDistortion);
            hdCameraData.DisableHDField(FrameSettingsField.ChromaticAberration);
            hdCameraData.DisableHDField(FrameSettingsField.Vignette);
            hdCameraData.DisableHDField(FrameSettingsField.FilmGrain);
            hdCameraData.DisableHDField(FrameSettingsField.Dithering);
            hdCameraData.DisableHDField(FrameSettingsField.Antialiasing);
            hdCameraData.DisableHDField(FrameSettingsField.Tonemapping);
            hdCameraData.DisableHDField(FrameSettingsField.LensFlareDataDriven);
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

        private static void SetPlayerOverrides(Camera camera, bool potato = false)
        {
            HDAdditionalCameraData hdCameraData = camera.GetComponent<HDAdditionalCameraData>();
            hdCameraData.customRenderingSettings = true;

            //hdCameraData.DisableHDField(FrameSettingsField.Volumetrics);

            // only potatoes beyond this point
            if (!potato) return;
            hdCameraData.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)FrameSettingsField.MSAAMode] = true;
            hdCameraData.renderingPathCustomFrameSettings.msaaMode = MSAAMode.None;
            hdCameraData.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)FrameSettingsField.MaterialQualityLevel] = true;
            hdCameraData.renderingPathCustomFrameSettings.materialQuality = MaterialQuality.Low;
            hdCameraData.DisableHDField(FrameSettingsField.Decals);
            hdCameraData.DisableHDField(FrameSettingsField.TransparentPrepass);
            hdCameraData.DisableHDField(FrameSettingsField.TransparentPostpass);
            hdCameraData.DisableHDField(FrameSettingsField.RayTracing);
            hdCameraData.DisableHDField(FrameSettingsField.MotionVectors);
            hdCameraData.DisableHDField(FrameSettingsField.Refraction);
            hdCameraData.DisableHDField(FrameSettingsField.Distortion);
            hdCameraData.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)FrameSettingsField.Postprocess] = true;
            hdCameraData.DisableHDField(FrameSettingsField.CustomPostProcess);
            hdCameraData.DisableHDField(FrameSettingsField.CustomPass);
            hdCameraData.DisableHDField(FrameSettingsField.StopNaN);
            hdCameraData.DisableHDField(FrameSettingsField.DepthOfField);
            hdCameraData.DisableHDField(FrameSettingsField.MotionBlur);
            hdCameraData.DisableHDField(FrameSettingsField.PaniniProjection);
            hdCameraData.DisableHDField(FrameSettingsField.Bloom);
            hdCameraData.DisableHDField(FrameSettingsField.LensDistortion);
            hdCameraData.DisableHDField(FrameSettingsField.ChromaticAberration);
            hdCameraData.DisableHDField(FrameSettingsField.Vignette);
            hdCameraData.DisableHDField(FrameSettingsField.FilmGrain);
            hdCameraData.DisableHDField(FrameSettingsField.Dithering);
            hdCameraData.DisableHDField(FrameSettingsField.Antialiasing);
            hdCameraData.DisableHDField(FrameSettingsField.LensFlareDataDriven);
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
            hdCameraData.DisableHDField(FrameSettingsField.ReflectionProbe);
            hdCameraData.DisableHDField(FrameSettingsField.PlanarProbe);
            hdCameraData.DisableHDField(FrameSettingsField.SkyReflection);
            hdCameraData.DisableHDField(FrameSettingsField.SubsurfaceScattering);
            hdCameraData.DisableHDField(FrameSettingsField.VolumetricClouds);
        }

        private static void DisableHDField(this HDAdditionalCameraData data, FrameSettingsField field)
        {
            data.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)field] = true;
            data.renderingPathCustomFrameSettings.SetEnabled(field, false);
        }
    }
}
