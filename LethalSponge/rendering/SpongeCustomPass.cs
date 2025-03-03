using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using Scoops.service;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Scoops.rendering
{
    class SpongeCustomPass : CustomPass
    {
        public static Material posterizationMaterial;
        public static Shader posterizationShader;
        public static RTHandle posterizationRT;

        public override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            if (Config.useWIPCustomShader.Value)
            {
                posterizationShader = (Shader)Plugin.SpongeAssets.LoadAsset("SpongePosterizeWIP");
            }
            else
            {
                posterizationShader = (Shader)Plugin.SpongeAssets.LoadAsset("SpongePosterize");
            }
                
            posterizationMaterial = CoreUtils.CreateEngineMaterial(posterizationShader);

            posterizationRT = RTHandles.Alloc(
                Vector2.one, TextureXR.slices, dimension: TextureXR.dimension,
                colorFormat: GraphicsFormat.R8G8B8A8_SRGB,
                useDynamicScale: true, name: "Posterization Buffer"
            );
        }

        public override void Execute(CustomPassContext ctx) 
        {
            ctx.propertyBlock.SetTexture("_SpongeCameraColorBuffer", ctx.cameraColorBuffer, RenderTextureSubElement.Color);

            CoreUtils.SetRenderTarget(ctx.cmd, posterizationRT, ClearFlag.All);
            CoreUtils.DrawFullScreen(ctx.cmd, posterizationMaterial, ctx.propertyBlock, posterizationMaterial.FindPass("ReadColor"));

            ctx.propertyBlock.SetTexture("_PosterizationBuffer", posterizationRT);

            CoreUtils.SetRenderTarget(ctx.cmd, ctx.cameraColorBuffer, ClearFlag.None);
            CoreUtils.DrawFullScreen(ctx.cmd, posterizationMaterial, ctx.propertyBlock, posterizationMaterial.FindPass("WriteColor"));
        }

        public override void Cleanup()
        {
            CoreUtils.Destroy(posterizationMaterial);
            posterizationRT.Release();
        }
    }
}
