using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Scoops.patches
{
    [HarmonyPatch(typeof(HDRenderPipeline))]
    [HarmonyPatch("RecordRenderGraph")]
    [HarmonyDebug]
    public static class HDRenderPipeline_RecordRenderGraph_Patch
    {
        static readonly List<CodeInstruction> instructionsToInsert = new List<CodeInstruction>() {
            // IL_06BF: ldarg.0
            new CodeInstruction(OpCodes.Ldarg_0),
            // IL_06C0: ldfld  class [Unity.RenderPipelines.Core.Runtime]UnityEngine.Experimental.Rendering.RenderGraphModule.RenderGraph UnityEngine.Rendering.HighDefinition.HDRenderPipeline::m_RenderGraph
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(HDRenderPipeline), nameof(HDRenderPipeline.m_RenderGraph))),
            // IL_06C5: ldloc.1
            new CodeInstruction(OpCodes.Ldloc_1),
            // IL_06C6: ldloc.s   V_8
            new CodeInstruction(OpCodes.Ldloc_S, 8),
            // IL_06C8: ldloca.s  V_13
            new CodeInstruction(OpCodes.Ldloca_S, 13),
            // IL_06CA: ldloc.3
            new CodeInstruction(OpCodes.Ldloc_3),
            // IL_06CB: ldloc.2
            new CodeInstruction(OpCodes.Ldloc_2),
            // IL_06CC: ldc.i4.6
            new CodeInstruction(OpCodes.Ldc_I4_7),
            // IL_06CD: ldarg.2
            new CodeInstruction(OpCodes.Ldarg_2),
            // IL_06CE: ldarg.s   aovCustomPassBuffers
            new CodeInstruction(OpCodes.Ldarg_S, 4),
            // IL_06D0: call      instance bool UnityEngine.Rendering.HighDefinition.HDRenderPipeline::RenderCustomPass(class [Unity.RenderPipelines.Core.Runtime]UnityEngine.Experimental.Rendering.RenderGraphModule.RenderGraph, class UnityEngine.Rendering.HighDefinition.HDCamera, valuetype [Unity.RenderPipelines.Core.Runtime]UnityEngine.Experimental.Rendering.RenderGraphModule.TextureHandle, valuetype UnityEngine.Rendering.HighDefinition.HDRenderPipeline/PrepassOutput&, valuetype [UnityEngine.CoreModule]UnityEngine.Rendering.CullingResults, valuetype [UnityEngine.CoreModule]UnityEngine.Rendering.CullingResults, valuetype UnityEngine.Rendering.HighDefinition.CustomPassInjectionPoint, valuetype UnityEngine.Rendering.HighDefinition.AOVRequestData, class [netstandard]System.Collections.Generic.List`1<class [Unity.RenderPipelines.Core.Runtime]UnityEngine.Rendering.RTHandle>)
            CodeInstruction.Call(typeof(HDRenderPipeline), nameof(HDRenderPipeline.RenderCustomPass)),
            // IL_06D5: pop
            new CodeInstruction(OpCodes.Pop),
            // IL_06BE: ldarg.0
            new CodeInstruction(OpCodes.Ldarg_0),
        };

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = new List<CodeInstruction>(instructions);

            int insertionIndex = -1;
            for (int i = 0; i < code.Count - 1; i++) // -1 since we will be checking i + 1
            {
                if (code[i].opcode == OpCodes.Call && code[i].Calls(AccessTools.Method(typeof(HDRenderPipeline), nameof(HDRenderPipeline.RenderSubsurfaceScattering))) && code[i + 1].opcode == OpCodes.Ldarg_0)
                {
                    insertionIndex = i + 2;
                    break;
                }
            }

            if (insertionIndex != -1)
            {
                code.InsertRange(insertionIndex, instructionsToInsert);
            }

            return code;
        }
    }
}
