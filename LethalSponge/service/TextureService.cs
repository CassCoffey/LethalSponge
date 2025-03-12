using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Scoops.service
{
    public static class TextureService
    {
        public static Dictionary<string, Texture2D> ResizedTextures = new Dictionary<string, Texture2D>();

        public static void ResizeAllTextures()
        {
            //Renderer[] allRenderers = GameObject.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Renderer[] allRenderers = Resources.FindObjectsOfTypeAll<Renderer>();

            foreach(Renderer renderer in allRenderers)
            {
                foreach(Material material in renderer.sharedMaterials)
                {
                    if (material != null)
                    {
                        Shader materialShader = material.shader;
                        if (materialShader != null)
                        {
                            for (int i = 0; i < materialShader.GetPropertyCount(); i++)
                            {
                                if (materialShader.GetPropertyType(i) == UnityEngine.Rendering.ShaderPropertyType.Texture)
                                {
                                    Texture texture = material.GetTexture(materialShader.GetPropertyName(i));
                                    if (texture != null && texture is Texture2D)
                                    {
                                        try
                                        {
                                            if (ResizedTextures.TryGetValue(texture.name, out Texture2D resizedTex))
                                            {
                                                material.SetTexture(materialShader.GetPropertyName(i), resizedTex);
                                                Resources.UnloadAsset(texture);
                                            }
                                            else if (texture.height > Config.maxTextureSize.Value || texture.width > Config.maxTextureSize.Value)
                                            {
                                                material.SetTexture(materialShader.GetPropertyName(i), GetResizedTexture((Texture2D)texture));
                                                Resources.UnloadAsset(texture);
                                            }
                                        }
                                        catch (Exception e)
                                        {
                                            Plugin.Log.LogWarning("Error while resizing Texture " + texture.name + ", continuing:");
                                            Plugin.Log.LogWarning(e);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public static Texture2D GetResizedTexture(Texture2D texture)
        {
            float largerDimension = texture.height > texture.width ? texture.height : texture.width;

            float scale = (float)Config.maxTextureSize.Value / largerDimension;

            int width = Mathf.RoundToInt(texture.width * scale);
            int height = Mathf.RoundToInt(texture.height * scale);

            RenderTexture rt = RenderTexture.GetTemporary(width, height, 0, UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_SRGB);

            Graphics.Blit(texture, rt);

            RenderTexture.active = rt;
            Texture2D result = new Texture2D(width, height, UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_SRGB, UnityEngine.Experimental.Rendering.TextureCreationFlags.None);
            result.name = texture.name + "_resized";
            result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            result.Apply(false, true);
            RenderTexture.ReleaseTemporary(rt);

            ResizedTextures.Add(texture.name, result);

            return result;
        }
    }
}
