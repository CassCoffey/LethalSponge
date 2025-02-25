using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace Scoops.rendering
{
    public class MainCamDepthCopy : CustomPass
    {
        public RTHandle shadowMapAtlas;

        public override void Execute(CustomPassContext ctx)
        {
            CustomPassUtils.Copy(ctx, ctx.cameraDepthBuffer, ctx.customDepthBuffer.Value);

            HDRenderPipeline pipeline = (HDRenderPipeline)RenderPipelineManager.currentPipeline;
            //CustomPassUtils.Copy(ctx, pipeline.m_ShadowManager.m_Atlas.GetOutputTexture(pipeline.m_RenderGraph), shadowMapAtlas);
        }
    }

    public class VolumetricCamDepthWrite : CustomPass
    {
        public override void Execute(CustomPassContext ctx)
        {
            CustomPassUtils.Copy(ctx, ctx.customDepthBuffer.Value, ctx.cameraDepthBuffer);
        }
    }

    public class VolumetricCamOverlay : CustomPass
    {
        public Material volumetricPassMat;
        public RTHandle volumetricRT;
        public RenderTexture mainRT;

        readonly int cameraColor = Shader.PropertyToID("_ColorTexture");

        public override void Execute(CustomPassContext ctx)
        {
            //HDShadowManager.BindAtlasTexture(ctx, , HDShaderIDs._ShadowmapAtlas);
            //volumetricPassMat.SetTexture(cameraColor, ctx.cameraColorBuffer);

            Graphics.Blit(volumetricRT, mainRT);

            //CoreUtils.SetRenderTarget(ctx.cmd, mainRenderTex.colorBuffer);
            //CoreUtils.DrawFullScreen(ctx.cmd, volumetricPassMat, volumetricPassMat.FindPass("ColorCopy"));
        }
    }
}