using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Scoops.service
{
    public static class TextureService
    {
        public static Dictionary<string, Texture2D> TextureDict = new Dictionary<string, Texture2D>();
        public static List<Texture2D> dupedTextures = new List<Texture2D>();

        public static string[] deDupeBlacklist;

        public static void ResizeAllTextures()
        {
            Material[] allMaterials = Resources.FindObjectsOfTypeAll<Material>();
            foreach (Material material in allMaterials)
            {
                if (material != null)
                {
                    Shader materialShader = material.shader;
                    if (materialShader != null && (materialShader.name != "TextMeshPro/Distance Field" && materialShader.name != "TextMeshPro/Mobile/Distance Field"))
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
                                        if (TextureDict.TryGetValue(texture.name, out Texture2D processedTex))
                                        {
                                            if (processedTex.GetInstanceID() == texture.GetInstanceID())
                                            {
                                                // Already processed
                                            }
                                            else
                                            {
                                                material.SetTexture(materialShader.GetPropertyName(i), processedTex);
                                                dupedTextures.Add((Texture2D)texture);
                                            }
                                        }
                                        else if (Config.resizeTextures.Value && (texture.height > Config.maxTextureSize.Value || texture.width > Config.maxTextureSize.Value))
                                        {
                                            Texture2D resizedTex = GetResizedTexture((Texture2D)texture);
                                            material.SetTexture(materialShader.GetPropertyName(i), resizedTex);
                                            AddToTextureDict(texture.name, resizedTex);
                                            dupedTextures.Add((Texture2D)texture);
                                        }
                                        else
                                        {
                                            AddToTextureDict(texture.name, (Texture2D)texture);
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

            foreach (Texture2D dupedTex in dupedTextures)
            {
                GameObject.Destroy(dupedTex);
                Resources.UnloadAsset(dupedTex);
            }

            dupedTextures = [];
        }

        public static void AddToTextureDict(string name, Texture2D tex)
        {
            if (name == "" || deDupeBlacklist.Contains(name)) return;
            TextureDict.Add(name, tex);
        }

        public static Texture2D GetResizedTexture(Texture2D texture)
        {
            float largerDimension = texture.height > texture.width ? texture.height : texture.width;

            float scale = (float)Config.maxTextureSize.Value / largerDimension;

            int width = Mathf.RoundToInt(texture.width * scale);
            int height = Mathf.RoundToInt(texture.height * scale);

            GraphicsFormat format = GraphicsFormat.R8G8B8A8_SRGB;

            RenderTexture rt = RenderTexture.GetTemporary(width, height, 0, format);

            Graphics.Blit(texture, rt);

            RenderTexture.active = rt;
            Texture2D result = new Texture2D(width, height, format, TextureCreationFlags.None);
            result.name = texture.name;
            result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            result.Apply(false, true);
            RenderTexture.ReleaseTemporary(rt);

            return result;
        }
    }
}
