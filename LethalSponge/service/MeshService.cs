using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Unity.Netcode;
using Unity.Properties;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityMeshSimplifier;

namespace Scoops.service
{
    public class MeshInfo
    {
        public string name;
        public int vertices;

        public MeshInfo(string name, int vertices)
        {
            this.name = name;
            this.vertices = vertices;
        }

        public override bool Equals(object obj) => this.Equals(obj as MeshInfo);

        public bool Equals(MeshInfo m)
        {
            if (m is null)
            {
                return false;
            }
            if (System.Object.ReferenceEquals(this, m))
            {
                return true;
            }
            if (this.GetType() != m.GetType())
            {
                return false;
            }

            return (name == m.name) && (vertices == m.vertices);
        }

        public override int GetHashCode() => (name, vertices).GetHashCode();

        public static bool operator ==(MeshInfo lhs, MeshInfo rhs)
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

        public static bool operator !=(MeshInfo lhs, MeshInfo rhs) => !(lhs == rhs);
    }

    public static class MeshService
    {
        public static Dictionary<int, Mesh> decimatedMeshDict = new Dictionary<int, Mesh>();
        public static Dictionary<MeshInfo, Mesh> lodMeshDict = new Dictionary<MeshInfo, Mesh>();
        public static HashSet<int> decimatedMeshes = new HashSet<int>();
        public static HashSet<int> generatedLODs = new HashSet<int>();

        public static Dictionary<MeshInfo, Mesh> MeshDict = new Dictionary<MeshInfo, Mesh>();
        public static List<Mesh> dupedMesh = new List<Mesh>();

        public static LayerMask LODlayers = LayerMask.GetMask("Props", "Default", "Enemies");

        public static string[] deDupeBlacklist;

        private static SimplificationOptions simplificationOptions = SimplificationOptions.Default;
        private static LODLevel[] levels = null;

        private static MeshSimplifier meshSimplifier;

        private static bool initialized = false;

        public static void Init()
        {
            simplificationOptions = new SimplificationOptions
            {
                PreserveBorderEdges = true,
                PreserveUVSeamEdges = true,
                PreserveUVFoldoverEdges = false,
                PreserveSurfaceCurvature = Config.preserveSurfaceCurvature.Value,
                EnableSmartLink = true,
                VertexLinkDistance = double.Epsilon,
                MaxIterationCount = 100,
                Agressiveness = 7.0,
                ManualUVComponentCount = false,
                UVComponentCount = 2
            };

            if (Config.useLOD2.Value)
            {
                levels = new LODLevel[]
                {
                    new LODLevel(Config.LOD1Start.Value, 1f)
                    {
                        CombineMeshes = false,
                        CombineSubMeshes = false,
                        SkinQuality = SkinQuality.Auto,
                        ShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On,
                        ReceiveShadows = true,
                        SkinnedMotionVectors = true,
                        LightProbeUsage = UnityEngine.Rendering.LightProbeUsage.BlendProbes,
                        ReflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.BlendProbes,
                    },
                    new LODLevel(Config.LOD2Start.Value, Config.LOD1Quality.Value)
                    {
                        CombineMeshes = true,
                        CombineSubMeshes = false,
                        SkinQuality = SkinQuality.Auto,
                        ShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On,
                        ReceiveShadows = true,
                        SkinnedMotionVectors = true,
                        LightProbeUsage = UnityEngine.Rendering.LightProbeUsage.BlendProbes,
                        ReflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Simple,
                    },
                    new LODLevel(Config.cullStart.Value, Config.LOD2Quality.Value)
                    {
                        CombineMeshes = true,
                        CombineSubMeshes = true,
                        SkinQuality = SkinQuality.Bone2,
                        ShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off,
                        ReceiveShadows = false,
                        SkinnedMotionVectors = false,
                        LightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off,
                        ReflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off,
                    }
                };
            }
            else
            {
                levels = new LODLevel[]
                {
                    new LODLevel(Config.LOD1Start.Value, 1f)
                    {
                        CombineMeshes = false,
                        CombineSubMeshes = false,
                        SkinQuality = SkinQuality.Auto,
                        ShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On,
                        ReceiveShadows = true,
                        SkinnedMotionVectors = true,
                        LightProbeUsage = UnityEngine.Rendering.LightProbeUsage.BlendProbes,
                        ReflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.BlendProbes,
                    },
                    new LODLevel(Config.cullStart.Value, Config.LOD1Quality.Value)
                    {
                        CombineMeshes = true,
                        CombineSubMeshes = false,
                        SkinQuality = SkinQuality.Auto,
                        ShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On,
                        ReceiveShadows = true,
                        SkinnedMotionVectors = true,
                        LightProbeUsage = UnityEngine.Rendering.LightProbeUsage.BlendProbes,
                        ReflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Simple,
                    }
                };
            }

            meshSimplifier = new MeshSimplifier();
            meshSimplifier.SimplificationOptions = simplificationOptions;

            initialized = true;
        }

        public static void DedupeAllMeshes()
        {
            MeshFilter[] allMeshFilters = Resources.FindObjectsOfTypeAll<MeshFilter>();
            foreach (MeshFilter meshFilter in allMeshFilters)
            {
                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    MeshInfo meshInfo = new MeshInfo(meshFilter.sharedMesh.name, meshFilter.sharedMesh.vertexCount);
                    if (MeshDict.TryGetValue(meshInfo, out Mesh processedMesh))
                    {
                        if (processedMesh.GetInstanceID() == meshFilter.sharedMesh.GetInstanceID())
                        {
                            // Already processed
                        }
                        else
                        {
                            dupedMesh.Add(meshFilter.sharedMesh);
                            meshFilter.sharedMesh = processedMesh;
                        }
                    }
                    else
                    {
                        AddToMeshDict(meshInfo, meshFilter.sharedMesh);
                    }
                }
            }

            // Skinned meshes cause too many problems at the moment
            //SkinnedMeshRenderer[] allSkinnedMeshRenderers = Resources.FindObjectsOfTypeAll<SkinnedMeshRenderer>();
            //foreach (SkinnedMeshRenderer skinnedMeshRenderer in allSkinnedMeshRenderers)
            //{
            //    if (skinnedMeshRenderer != null && skinnedMeshRenderer.sharedMesh != null)
            //    {
            //        MeshInfo meshInfo = new MeshInfo(skinnedMeshRenderer.sharedMesh.name, skinnedMeshRenderer.sharedMesh.vertexCount);
            //        if (MeshDict.TryGetValue(meshInfo, out Mesh processedMesh) && processedMesh.GetInstanceID() == skinnedMeshRenderer.sharedMesh.GetInstanceID())
            //        {
            //            // Already processed
            //        }
            //        else if (MeshDict.TryGetValue(meshInfo, out Mesh dedupedMesh))
            //        {
            //            dupedMesh.Add(skinnedMeshRenderer.sharedMesh);
            //            skinnedMeshRenderer.sharedMesh = dedupedMesh;
            //        }
            //        else
            //        {
            //            AddToMeshDict(meshInfo, skinnedMeshRenderer.sharedMesh);
            //        }
            //    }
            //}

            MeshCollider[] allMeshColliders = Resources.FindObjectsOfTypeAll<MeshCollider>();
            foreach (MeshCollider meshCollider in allMeshColliders)
            {
                if (meshCollider != null && meshCollider.sharedMesh != null)
                {
                    MeshInfo meshInfo = new MeshInfo(meshCollider.sharedMesh.name, meshCollider.sharedMesh.vertexCount);
                    if (MeshDict.TryGetValue(meshInfo, out Mesh processedMesh))
                    {
                        if (processedMesh.GetInstanceID() == meshCollider.sharedMesh.GetInstanceID())
                        {
                            // Already processed
                        }
                        else
                        {
                            dupedMesh.Add(meshCollider.sharedMesh);
                            meshCollider.sharedMesh = processedMesh;
                        }
                    }
                    else
                    {
                        AddToMeshDict(meshInfo, meshCollider.sharedMesh);
                    }
                }
            }

            CleanDupedMeshes();
        }

        public static void AddToMeshDict(MeshInfo info, Mesh mesh)
        {
            if (info.name == "" || deDupeBlacklist.Contains(info.name)) return;
            MeshDict.Add(info, mesh);
        }

        public static void CleanDupedMeshes()
        {
            foreach (Mesh dupedMesh in dupedMesh)
            {
                GameObject.Destroy(dupedMesh);
                Resources.UnloadAsset(dupedMesh);
            }

            dupedMesh = [];
        }

        public static void GenerateLODs(GameObject gameObject, Transform root = null)
        {
            // Ignore objects we've already generated LODs for
            if (generatedLODs.Contains(gameObject.GetInstanceID())) return;
            if (gameObject.transform.Find(LODGenerator.LODParentGameObjectName) != null) return;
            if (root != null && root.Find(LODGenerator.LODParentGameObjectName) != null) return;

            // Ignore objects that already have LODs
            LODGroup[] existingLODs = gameObject.GetComponentsInChildren<LODGroup>();
            if (existingLODs.Length > 0) return;

            try
            {
                LODGenerator.GenerateLODs(gameObject, levels, true, simplificationOptions, root);
                generatedLODs.Add(gameObject.GetInstanceID());
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning(e);
            }
        }

        public static void DecimateAllMeshes(GameObject gameObject)
        {
            MeshFilter[] allMeshFilters = gameObject.GetComponentsInChildren<MeshFilter>();

            foreach (MeshFilter meshFilter in allMeshFilters)
            {
                Mesh sourceMesh = meshFilter.sharedMesh;
                if (sourceMesh && sourceMesh.vertexCount > 1000 && !meshFilter.gameObject.GetComponent<SkinnedMeshRenderer>())
                {
                    int sourceId = sourceMesh.GetInstanceID();

                    // Skip meshes we've already set
                    if (decimatedMeshes.Contains(sourceId)) continue;

                    if (decimatedMeshDict.TryGetValue(sourceId, out Mesh generatedMesh))
                    {
                        meshFilter.sharedMesh = generatedMesh;
                    } 
                    else
                    {
                        if (!sourceMesh.isReadable) sourceMesh = GetReadableMesh(sourceMesh);
                        if (sourceMesh == null) continue;

                        Mesh newMesh = null;
                        float quality = 1f;

                        Renderer renderer = meshFilter.gameObject.GetComponent<Renderer>();
                        float vertCutoff = Config.complexMeshVertCutoff.Value;
                        if (renderer != null && sourceMesh.vertexCount > vertCutoff)
                        {
                            float largestDimension = Mathf.Max(renderer.bounds.size.x, Mathf.Max(renderer.bounds.size.y, renderer.bounds.size.z));
                            float cubedMeters = largestDimension * largestDimension * largestDimension;
                            float vertPerMeter = (float)sourceMesh.vertexCount / cubedMeters;

                            if (vertPerMeter >= vertCutoff)
                            {
                                quality = Mathf.Clamp(vertCutoff / vertPerMeter, 0.2f, 1f);
                            }
                        }
                        
                        if (quality == 1f)
                        {
                            continue;
                        } 
                        else
                        {
                            newMesh = DecimateMesh(sourceMesh, quality);
                            newMesh.name = sourceMesh.name;
                            newMesh.UploadMeshData(true);
                        }

                        decimatedMeshDict[sourceId] = newMesh;
                        decimatedMeshes.Add(newMesh.GetInstanceID());
                        dupedMesh.Add(meshFilter.sharedMesh);
                        meshFilter.sharedMesh = newMesh;
                    }
                }
            }

            CleanDupedMeshes();
        }

        public static Mesh DecimateMeshLossless(Mesh sourceMesh)
        {
            meshSimplifier.Initialize(sourceMesh);

            meshSimplifier.SimplifyMeshLossless();

            return meshSimplifier.ToMesh();
        }

        public static Mesh DecimateMesh(Mesh sourceMesh, float quality)
        {
            meshSimplifier.Initialize(sourceMesh);

            meshSimplifier.SimplifyMesh(quality);

            return meshSimplifier.ToMesh();
        }

        public static Mesh GetReadableMesh(Mesh nonReadableMesh)
        {
            try
            {
                Mesh meshCopy = new Mesh();
                meshCopy.indexFormat = nonReadableMesh.indexFormat;

                // Handle vertices
                GraphicsBuffer verticesBuffer = nonReadableMesh.GetVertexBuffer(0);
                int totalSize = verticesBuffer.stride * verticesBuffer.count;
                byte[] data = new byte[totalSize];
                verticesBuffer.GetData(data);
                meshCopy.SetVertexBufferParams(nonReadableMesh.vertexCount, nonReadableMesh.GetVertexAttributes());
                meshCopy.SetVertexBufferData(data, 0, 0, totalSize);
                verticesBuffer.Release();

                // Handle triangles
                meshCopy.subMeshCount = nonReadableMesh.subMeshCount;
                GraphicsBuffer indexesBuffer = nonReadableMesh.GetIndexBuffer();
                int tot = indexesBuffer.stride * indexesBuffer.count;
                byte[] indexesData = new byte[tot];
                indexesBuffer.GetData(indexesData);
                meshCopy.SetIndexBufferParams(indexesBuffer.count, nonReadableMesh.indexFormat);
                meshCopy.SetIndexBufferData(indexesData, 0, 0, tot);
                indexesBuffer.Release();

                // Restore submesh structure
                uint currentIndexOffset = 0;
                for (int i = 0; i < meshCopy.subMeshCount; i++)
                {
                    uint subMeshIndexCount = nonReadableMesh.GetIndexCount(i);
                    meshCopy.SetSubMesh(i, new SubMeshDescriptor((int)currentIndexOffset, (int)subMeshIndexCount));
                    currentIndexOffset += subMeshIndexCount;
                }

                // Recalculate normals and bounds
                meshCopy.RecalculateNormals();
                meshCopy.RecalculateBounds();

                return meshCopy;
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Error while reading mesh " + nonReadableMesh.name + ", continuing:");
                Plugin.Log.LogWarning(e);

                return null;
            }
        }

        public static void ProcessGrabbableObject(GrabbableObject grabbableObject)
        {
            ProcessGameObject(grabbableObject.gameObject, grabbableObject.mainObjectRenderer.transform);
        }

        public static void ProcessGameObject(GameObject gameObject, Transform root = null, bool generateLODs = true)
        {
            if (!initialized)
            {
                Init();
            }

            if (Config.fixComplexMeshes.Value)
            {
                DecimateAllMeshes(gameObject);
            }
            if (Config.generateLODs.Value && generateLODs)
            {
                GenerateLODs(gameObject, root);
            }
        }

        public static void ProcessScene(Scene scene, LoadSceneMode loadSceneMode)
        {
            foreach (GameObject rootObject in scene.GetRootGameObjects())
            {
                GrabbableObject[] grabbableObjects = rootObject.GetComponentsInChildren<GrabbableObject>();
                foreach (GrabbableObject grabbable in grabbableObjects)
                {
                    ProcessGameObject(grabbable.gameObject);
                }
            }
        }
    }

    [HarmonyPatch]
    class GrabbableObjectPatches
    {
        static IEnumerable<MethodBase> TargetMethods() => new[]
        {
            AccessTools.Method(typeof(GrabbableObject), "Start"),
        };

        static void Postfix(ref GrabbableObject __instance)
        {
            MeshService.ProcessGameObject(__instance.gameObject, __instance.mainObjectRenderer ? __instance.mainObjectRenderer.transform : null);
        }
    }
}
