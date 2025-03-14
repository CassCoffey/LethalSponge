using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Scoops.service
{
    public class TextureInfo
    {
        public string name;
        public int width;
        public int height;
        public GraphicsFormat format;

        public TextureInfo(Texture2D texture)
        {
            this.name = texture.name;
            this.width = texture.width;
            this.height = texture.height;
            this.format = texture.graphicsFormat;
        }

        public override bool Equals(object obj) => this.Equals(obj as TextureInfo);

        public bool Equals(TextureInfo t)
        {
            if (t is null)
            {
                return false;
            }
            if (System.Object.ReferenceEquals(this, t))
            {
                return true;
            }
            if (this.GetType() != t.GetType())
            {
                return false;
            }

            return (name == t.name) && (width == t.width) && (height == t.height) && (format == t.format);
        }

        public override int GetHashCode() => (name, width, height, format).GetHashCode();

        public static bool operator ==(TextureInfo lhs, TextureInfo rhs)
        {
            if (lhs is null)
            {
                if (rhs is null)
                {
                    return true;
                }

                // Only the left side is null.
                return false;
            }
            // Equals handles case of null on right side.
            return lhs.Equals(rhs);
        }

        public static bool operator !=(TextureInfo lhs, TextureInfo rhs) => !(lhs == rhs);
    }

    public static class TextureService
    {
        public static Dictionary<TextureInfo, Texture2D> TextureDict = new Dictionary<TextureInfo, Texture2D>();
        public static List<Texture2D> dupedTextures = new List<Texture2D>();

        public static string[] deDupeBlacklist;

        public static void ResizeAllTextures()
        {
            Texture2D[] allTextures = Resources.FindObjectsOfTypeAll<Texture2D>();
            Array.Sort(allTextures, delegate (Texture2D x, Texture2D y) {
                int id1 = x.GetInstanceID();
                uint id1ordered = id1 < 0 ? (uint)Math.Abs(id1) + (uint)int.MaxValue : (uint)id1;
                int id2 = y.GetInstanceID();
                uint id2ordered = id2 < 0 ? (uint)Math.Abs(id2) + (uint)int.MaxValue : (uint)id2;
                return (id1ordered).CompareTo(id2ordered);
            });

            foreach (Texture2D texture in allTextures)
            {
                if (texture != null)
                {
                    TextureInfo textureInfo = new TextureInfo(texture);
                    if (!TextureDict.TryGetValue(textureInfo, out Texture2D original))
                    {
                        if (Config.resizeTextures.Value && (texture.height > Config.maxTextureSize.Value || texture.width > Config.maxTextureSize.Value))
                        {
                            try
                            {
                                Texture2D resizedTex = GetResizedTexture(texture);
                                AddToTextureDict(textureInfo, resizedTex);
                            }
                            catch (Exception e)
                            {
                                Plugin.Log.LogWarning("Error while resizing Texture " + texture.name + ", continuing:");
                                Plugin.Log.LogWarning(e);
                            }
                        } 
                        else if (Config.deDupeTextures.Value)
                        {
                            AddToTextureDict(textureInfo, texture);
                        }
                    }
                }
            }

            Material[] allMaterials = Resources.FindObjectsOfTypeAll<Material>();
            //Array.Sort(allMaterials, delegate (Material x, Material y) {
            //    int id1 = x.GetInstanceID();
            //    uint id1ordered = id1 < 0 ? (uint)Math.Abs(id1) + (uint)int.MaxValue : (uint)id1;
            //    int id2 = y.GetInstanceID();
            //    uint id2ordered = id2 < 0 ? (uint)Math.Abs(id2) + (uint)int.MaxValue : (uint)id2;
            //    return (id1ordered).CompareTo(id2ordered);
            //});

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
                                    TextureInfo textureInfo = new TextureInfo((Texture2D)texture);
                                    if (TextureDict.TryGetValue(textureInfo, out Texture2D processedTex))
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

            dupedTextures.Clear();
            TextureDict.Clear();
        }

        public static void AddToTextureDict(TextureInfo info, Texture2D tex)
        {
            if (info.name == "" || deDupeBlacklist.Contains(info.name.ToLower())) return;
            TextureDict.Add(info, tex);
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
