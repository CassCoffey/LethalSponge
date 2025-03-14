using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Scoops.service
{
    public struct ShaderPropertyInfo
    {
        public string name;
        public ShaderPropertyType type;
        public Color _color;
        public float _float;
        public int _int;
        public Texture _texture;
        public Vector4 _vector;
    }

    public class ShaderInfo
    {
        public string name;
        public int passCount;
        public int subshaderCount;
        public int renderQueue;
        public int propertyCount;
        public string propertyNames;
        public bool alphaCutoff;
        public int surfaceType;
        public uint keywordCount;
    
        public ShaderInfo(Shader shader)
        {
            this.name = shader.name;
            this.passCount = shader.passCount;
            this.subshaderCount = shader.subshaderCount;
            this.renderQueue = shader.renderQueue;
            this.propertyCount = shader.GetPropertyCount();
            propertyNames = "";
            for (int i = 0; i < propertyCount; i++)
            {
                propertyNames += shader.GetPropertyName(i);
            }
            this.keywordCount = shader.keywordSpace.keywordCount;
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
    
            return (name == s.name) && (passCount == s.passCount) && (subshaderCount == s.subshaderCount) && (renderQueue == s.renderQueue) && (propertyCount == s.propertyCount) && (propertyNames == s.propertyNames) && (keywordCount == s.keywordCount);
        }
    
        public override int GetHashCode() => (name, passCount, subshaderCount, renderQueue, propertyCount, propertyNames).GetHashCode();
    
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
            Array.Sort(allMaterials, delegate(Material x, Material y) {
                int id1 = x.shader ? x.shader.GetInstanceID() : 0;
                uint id1ordered = id1 < 0 ? (uint)Math.Abs(id1) + (uint)int.MaxValue : (uint)id1;
                int id2 = y.shader ? y.shader.GetInstanceID() : 0;
                uint id2ordered = id2 < 0 ? (uint)Math.Abs(id2) + (uint)int.MaxValue : (uint)id2;
                return (id1ordered).CompareTo(id2ordered); 
            });

            foreach (Material material in allMaterials)
            {
                if (material != null && material.shader != null)
                {
                    ShaderInfo shaderInfo = new ShaderInfo(material.shader);
                    if (ShaderDict.TryGetValue(shaderInfo, out Shader processedShader))
                    {
                        if (processedShader.GetInstanceID() == material.shader.GetInstanceID())
                        {
                            // Already processed
                        }
                        else
                        {
                            dupedShader.Add(material.shader);
                            material.shader = processedShader;
                        }
                    }
                    else
                    {
                        Plugin.Log.LogInfo("Original copy of " + material.shader.name + " with ID " + material.shader.GetInstanceID() + " from material " + material.name + "with ID " + material.GetInstanceID());
                        AddToShaderDict(shaderInfo, material.shader);
                    }
                }
            }

            foreach (Shader dupedShader in dupedShader)
            {
                GameObject.Destroy(dupedShader);
                Resources.UnloadAsset(dupedShader);
            }

            dupedShader.Clear();
            ShaderDict.Clear();
        }

        public static void AddToShaderDict(ShaderInfo info, Shader shader)
        {
            if (info.name == "" || deDupeBlacklist.Contains(info.name.ToLower())) return;
            ShaderDict.Add(info, shader);
        }
    }
}
