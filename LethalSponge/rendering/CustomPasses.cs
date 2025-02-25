using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

namespace Scoops.rendering
{
    public class MainCamDepthCopy : CustomPass
    {
        protected override void Execute(CustomPassContext ctx)
        {
            CustomPassUtils.Copy(ctx, ctx.cameraDepthBuffer, ctx.customDepthBuffer.Value);
        }
    }

    public class VolumetricCamDepthWrite : CustomPass
    {
        protected override void Execute(CustomPassContext ctx)
        {
            CustomPassUtils.Copy(ctx, ctx.customDepthBuffer.Value, ctx.cameraDepthBuffer);
        }
    }

    public class VolumetricCamOverlay : CustomPass
    {
        public Material volumetricPassMat;
        public RenderTexture volumetricRT;
        public RenderTexture mainRT;

        readonly int cameraColor = Shader.PropertyToID("_ColorTexture");

        protected override void Execute(CustomPassContext ctx)
        {
            //volumetricPassMat.SetTexture(cameraColor, ctx.cameraColorBuffer);

            Graphics.Blit(volumetricRT, mainRT);

            //CoreUtils.SetRenderTarget(ctx.cmd, mainRenderTex.colorBuffer);
            //CoreUtils.DrawFullScreen(ctx.cmd, volumetricPassMat, volumetricPassMat.FindPass("ColorCopy"));
        }
    }
}