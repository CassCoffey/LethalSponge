using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Scoops.service
{
    public static class ShaderService
    {
        public static Dictionary<string, Shader> ShaderDict = new Dictionary<string, Shader>();
        public static List<Shader> dupedShader = new List<Shader>();

        public static void DedupeAllShaders()
        {
            Material[] allMaterials = Resources.FindObjectsOfTypeAll<Material>();
            foreach (Material material in allMaterials)
            {
                if (material != null && material.shader != null)
                {
                    if (ShaderDict.TryGetValue(material.shader.name, out Shader processedShader) && processedShader.GetInstanceID() == material.shader.GetInstanceID())
                    {
                        // Already processed
                    }
                    else if (ShaderDict.TryGetValue(material.shader.name, out Shader dedupedShader))
                    {
                        dupedShader.Add(material.shader);
                        Material dummyMat = new Material(material);
                        material.shader = dedupedShader;
                        material.CopyPropertiesFromMaterial(dummyMat);
                        GameObject.Destroy(dummyMat);
                    }
                    else
                    {
                        AddToShaderDict(material.shader.name, material.shader);
                    }
                }
            }

            foreach (Shader dupedShader in dupedShader)
            {
                GameObject.Destroy(dupedShader);
                Resources.UnloadAsset(dupedShader);
            }

            dupedShader = [];
        }

        public static void AddToShaderDict(string name, Shader shader)
        {
            if (name == "" || Config.deDupeTextureBlacklist.Value.ToLower().Trim().Split(';').Contains(name)) return;
            ShaderDict.Add(name, shader);
        }
    }
}
