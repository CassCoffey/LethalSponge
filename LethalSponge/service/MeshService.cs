﻿using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Unity.Netcode;
using Unity.Properties;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using UnityMeshSimplifier;

namespace Scoops.service
{
    public class MeshInfo
    {
        public string name;
        public int vertices;
        public Vector3 boundsSize;
        public bool readable;

        public MeshInfo(Mesh mesh)
        {
            this.name = mesh.name;
            this.vertices = mesh.vertexCount;
            this.boundsSize = mesh.bounds.size;
            this.readable = mesh.isReadable;
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

            return (name == m.name) && (vertices == m.vertices) && (boundsSize == m.boundsSize) && (readable == m.readable);
        }

        public override int GetHashCode() => (name, vertices, boundsSize, readable).GetHashCode();

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
        public static Dictionary<int, int> generatedLODs = new Dictionary<int, int>();

        public static Dictionary<MeshInfo, Mesh> MeshDict = new Dictionary<MeshInfo, Mesh>();
        public static List<Mesh> dupedMesh = new List<Mesh>();

        public static LayerMask LODlayers = LayerMask.GetMask("Props", "Default", "Enemies");

        public static string[] deDupeBlacklist;
        public static string[] LODBlacklist;
        public static string[] fixComplexMeshBlacklist;
        public static string[] fixComplexGameObjectBlacklist;

        private static SimplificationOptions simplificationOptions = SimplificationOptions.Default;
        private static LODLevel[] levels = null;

        private static MeshSimplifier meshSimplifier;

        private static bool initialized = false;

        public static void Init()
        {
            LODBlacklist = Config.generateLODsBlacklist.Value.ToLower().Split(';');
            fixComplexMeshBlacklist = Config.fixComplexMeshesBlacklist.Value.ToLower().Split(';');
            fixComplexGameObjectBlacklist = Config.fixComplexMeshesGameObjectBlacklist.Value.ToLower().Split(';');

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

            if (Config.generateLODMeshes.Value)
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
            else
            {
                levels = new LODLevel[]
                {
                    new LODLevel(Config.cullStart.Value, 1f)
                    {
                        CombineMeshes = false,
                        CombineSubMeshes = false,
                        SkinQuality = SkinQuality.Auto,
                        ShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On,
                        ReceiveShadows = true,
                        SkinnedMotionVectors = true,
                        LightProbeUsage = UnityEngine.Rendering.LightProbeUsage.BlendProbes,
                        ReflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.BlendProbes,
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
            Array.Sort(allMeshFilters, delegate (MeshFilter x, MeshFilter y) {
                int id1 = x.sharedMesh ? x.sharedMesh.GetInstanceID() : 0;
                uint id1ordered = id1 < 0 ? (uint)Math.Abs(id1) + (uint)int.MaxValue : (uint)id1;
                int id2 = y.sharedMesh ? y.sharedMesh.GetInstanceID() : 0;
                uint id2ordered = id2 < 0 ? (uint)Math.Abs(id2) + (uint)int.MaxValue : (uint)id2;
                return (id1ordered).CompareTo(id2ordered);
            });

            foreach (MeshFilter meshFilter in allMeshFilters)
            {
                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    MeshInfo meshInfo = new MeshInfo(meshFilter.sharedMesh);
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
            Array.Sort(allMeshColliders, delegate (MeshCollider x, MeshCollider y) {
                int id1 = x.sharedMesh ? x.sharedMesh.GetInstanceID() : 0;
                uint id1ordered = id1 < 0 ? (uint)Math.Abs(id1) + (uint)int.MaxValue : (uint)id1;
                int id2 = y.sharedMesh ? y.sharedMesh.GetInstanceID() : 0;
                uint id2ordered = id2 < 0 ? (uint)Math.Abs(id2) + (uint)int.MaxValue : (uint)id2;
                return (id1ordered).CompareTo(id2ordered);
            });

            foreach (MeshCollider meshCollider in allMeshColliders)
            {
                // Only going to dedupe readable meshcolliders for now
                if (meshCollider != null && meshCollider.sharedMesh != null && meshCollider.sharedMesh.isReadable)
                {
                    MeshInfo meshInfo = new MeshInfo(meshCollider.sharedMesh);
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

            MeshDict.Clear();
        }

        public static void AddToMeshDict(MeshInfo info, Mesh mesh)
        {
            if (info.name == "" || deDupeBlacklist.Contains(info.name.ToLower())) return;

            MeshDict.Add(info, mesh);
        }

        public static void CleanDupedMeshes()
        {
            //foreach (Mesh dupedMesh in dupedMesh)
            //{
            //    Mesh.Destroy(dupedMesh);
            //    Resources.UnloadAsset(dupedMesh);
            //}

            dupedMesh.Clear();
        }

        public static void GenerateLODs(GameObject gameObject, Transform root = null, GrabbableObject grabbable = null)
        {
            // Ignore objects we've already generated LODs for
            if (generatedLODs.TryGetValue(gameObject.GetInstanceID(), out int value)) return;
            if (gameObject.transform.Find(LODGenerator.LODParentGameObjectName) != null) return;
            if (root != null && root.Find(LODGenerator.LODParentGameObjectName) != null) return;

            // Ignore objects that already have LODs
            LODGroup[] existingLODs = gameObject.GetComponentsInChildren<LODGroup>();
            if (existingLODs.Length > 0) return;

            // Ignore blacklisted gameobjects
            string name = gameObject.name.ToLower().Replace("(clone)", "").Replace(" (instance)", "");
            if (LODBlacklist.Contains(name)) return;

            try
            {
                LODGenerator.GenerateLODs(gameObject, levels, true, simplificationOptions, root);
                int hash = gameObject.GetHashCode();
                if (!ReferenceEquals(grabbable, null))
                {
                    hash = GetGrabbableHash(grabbable);
                }
                generatedLODs.Add(gameObject.GetInstanceID(), hash);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning(e);
            }
        }

        public static void RegenerateLODs(GrabbableObject grabbable, Transform root = null)
        {
            // Make sure we've already generated LODs for this object
            if (!generatedLODs.TryGetValue(grabbable.gameObject.GetInstanceID(), out int hash)) return;

            int newHash = GetGrabbableHash(grabbable);

            // Item is not changed
            if (newHash == hash) return;

            if (LODGenerator.DestroyLODs(grabbable.gameObject)) 
            {
                grabbable.StartCoroutine(RegenerateLODsCoroutine(grabbable.gameObject, root, newHash));
            }
        }

        public static IEnumerator RegenerateLODsCoroutine(GameObject gameObject, Transform root = null, int hash = 0)
        {
            // Wait 2 frames for previous LODs to be destroyed
            yield return 0;
            yield return 0;

            if (!gameObject.transform.Find(LODGenerator.LODParentGameObjectName))
            {
                try
                {
                    LODGenerator.GenerateLODs(gameObject, levels, true, simplificationOptions, root);
                    generatedLODs[gameObject.GetInstanceID()] = hash;
                }
                catch (Exception e)
                {
                    Plugin.Log.LogWarning(e);
                }
            }
        }

        private static int GetGrabbableHash(GrabbableObject grabbable)
        {
            if (grabbable.mainObjectRenderer == null) return grabbable.GetHashCode();

            MeshFilter filter = grabbable.mainObjectRenderer.GetComponent<MeshFilter>();
            int materialsHash = ((IStructuralEquatable)grabbable.mainObjectRenderer.sharedMaterials).GetHashCode(EqualityComparer<Material>.Default);
            int filterHash = filter ? filter.mesh.GetHashCode() : 0;
            return HashCode.Combine(materialsHash, filterHash);
        }

        public static void DecimateAllMeshes(GameObject gameObject)
        {
            if (fixComplexGameObjectBlacklist.Contains(gameObject.name.ToLower())) return;

            MeshFilter[] allMeshFilters = gameObject.GetComponentsInChildren<MeshFilter>();

            foreach (MeshFilter meshFilter in allMeshFilters)
            {
                Mesh sourceMesh = meshFilter.sharedMesh;
                float vertCutoff = Config.complexMeshVertCutoff.Value;
                if (sourceMesh && sourceMesh.vertexCount > vertCutoff && 
                    !meshFilter.gameObject.GetComponent<SkinnedMeshRenderer>() && 
                    !fixComplexMeshBlacklist.Contains(sourceMesh.name.ToLower()))
                {
                    int sourceId = sourceMesh.GetInstanceID();
                    string sourceName = sourceMesh.name;

                    // Skip meshes we've already set
                    if (decimatedMeshes.Contains(sourceId)) continue;

                    if (decimatedMeshDict.TryGetValue(sourceId, out Mesh generatedMesh))
                    {
                        meshFilter.sharedMesh = generatedMesh;
                    } 
                    else
                    {
                        Mesh newMesh = null;
                        float quality = 1f;

                        Renderer renderer = meshFilter.gameObject.GetComponent<Renderer>();
                        if (renderer != null && sourceMesh.vertexCount > vertCutoff)
                        {
                            float largestDimension = Mathf.Max(renderer.bounds.size.x, Mathf.Max(renderer.bounds.size.y, renderer.bounds.size.z));
                            float cubedMeters = largestDimension * largestDimension * largestDimension;
                            float vertPerMeter = (float)sourceMesh.vertexCount / cubedMeters;

                            if (vertPerMeter >= vertCutoff)
                            {
                                if (!Config.minimalLogging.Value)
                                {
                                    Plugin.Log.LogWarning("Found complex mesh with Mesh name: " + sourceName);
                                    Plugin.Log.LogWarning(" - and GameObject name: " + gameObject.name);
                                    Plugin.Log.LogWarning(" - with " + sourceMesh.vertexCount + " vertices");
                                }
                                quality = Mathf.Clamp(vertCutoff / vertPerMeter, 0.15f, 1f);
                            }
                        }
                        
                        if (quality == 1f)
                        {
                            continue;
                        } 
                        else
                        {
                            bool readable = sourceMesh.isReadable;
                            if (!readable)
                            {
                                sourceMesh = GetReadableMesh(sourceMesh);
                                sourceMesh.name = sourceName;
                            }
                            newMesh = DecimateMesh(sourceMesh, quality);
                            newMesh.name = sourceMesh.name;
                            newMesh.UploadMeshData(!readable);
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
            if (!initialized)
            {
                Init();
            }

            if (Config.fixComplexMeshes.Value && Config.fixComplexGrabbable.Value)
            {
                DecimateAllMeshes(grabbableObject.gameObject);
            }
            if (Config.generateLODs.Value)
            {
                GenerateLODs(grabbableObject.gameObject, grabbableObject.mainObjectRenderer ? grabbableObject.mainObjectRenderer.transform : null, grabbableObject);
            }
        }

        public static void ProcessGameObject(GameObject gameObject, Transform root = null)
        {
            if (!initialized)
            {
                Init();
            }

            if (Config.fixComplexMeshes.Value)
            {
                DecimateAllMeshes(gameObject);
            }
            if (Config.generateLODs.Value)
            {
                GenerateLODs(gameObject, root);
            }
        }
    }

    public static class GrabbableObjectPatches
    {
        [HarmonyPatch(typeof(RoundManager))]
        [HarmonyPatch("SyncScrapValuesClientRpc")]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        public static void RoundManager_SyncScrapValuesClientRpc(NetworkObjectReference[] spawnedScrap, int[] allScrapValue)
        {
            for (int i = 0; i < spawnedScrap.Length; i++)
            {
                NetworkObject networkObject;
                if (spawnedScrap[i].TryGet(out networkObject, null))
                {
                    GrabbableObject component = networkObject.GetComponent<GrabbableObject>();
                    if (component != null)
                    {
                        MeshService.RegenerateLODs(component, component.mainObjectRenderer ? component.mainObjectRenderer.transform : null);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(GrabbableObject))]
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        public static void GrabbableObject_Start(GrabbableObject __instance)
        {
            MeshService.ProcessGrabbableObject(__instance);
        }
    }
}
