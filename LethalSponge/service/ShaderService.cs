using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Scoops.service
{
    public class ShaderInfo
    {
        public string name;
        public int passCount;
        public int subshaderCount;
    
        public ShaderInfo(string name, int passCount, int subshaderCount)
        {
            this.name = name;
            this.passCount = passCount;
            this.subshaderCount = subshaderCount;
        }
    
        public override bool Equals(object obj) => this.Equals(obj as ShaderInfo);
    
        public bool Equals(ShaderInfo s)
        {
            if (s is null)
            {
                return false;
            }
            if (System.Object.ReferenceEquals(this, s))
            {
                return true;
            }
            if (this.GetType() != s.GetType())
            {
                return false;
            }
    
            return (name == s.name) && (passCount == s.passCount) && (subshaderCount == s.subshaderCount);
        }
    
        public override int GetHashCode() => (name, passCount, subshaderCount).GetHashCode();
    
        public static bool operator ==(ShaderInfo lhs, ShaderInfo rhs)
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
    
        public static bool operator !=(ShaderInfo lhs, ShaderInfo rhs) => !(lhs == rhs);
    }

    public static class ShaderService
    {
        public static Dictionary<ShaderInfo, Shader> ShaderDict = new Dictionary<ShaderInfo, Shader>();
        public static List<Shader> dupedShader = new List<Shader>();

        public static string[] deDupeBlacklist;

        public static void DedupeAllShaders()
        {
            Material[] allMaterials = Resources.FindObjectsOfTypeAll<Material>();
            foreach (Material material in allMaterials)
            {
                if (material != null && material.shader != null)
                {
                    ShaderInfo shaderInfo = new ShaderInfo(material.shader.name, material.shader.passCount, material.shader.subshaderCount);
                    if (ShaderDict.TryGetValue(shaderInfo, out Shader processedShader))
                    {
                        if (processedShader.GetInstanceID() == material.shader.GetInstanceID())
                        {
                            // Already processed
                        }
                        else
                        {
                            dupedShader.Add(material.shader);
                            Material dummyMat = new Material(material);
                            material.shader = processedShader;
                            material.CopyPropertiesFromMaterial(dummyMat);
                            material.enabledKeywords = dummyMat.enabledKeywords;
                            GameObject.Destroy(dummyMat);
                        }
                    }
                    else
                    {
                        AddToShaderDict(shaderInfo, material.shader);
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

        public static void AddToShaderDict(ShaderInfo info, Shader shader)
        {
            if (info.name == "" || deDupeBlacklist.Contains(info.name)) return;
            ShaderDict.Add(info, shader);
        }
    }
}
