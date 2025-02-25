using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

namespace Scoops.rendering
{
    public class VolumetricPostProcessCamera : MonoBehaviour
    {
        public RenderTexture volumetricsRT;
        public RenderTexture depthRT;
        public Camera volumetricsCam;

        private void OnEnable()
        {
            volumetricsCam = GetComponent<Camera>();
            volumetricsCam.clearFlags = CameraClearFlags.Color;
            volumetricsCam.SetTargetBuffers(volumetricsRT.colorBuffer, depthRT.depthBuffer);
        }

        private void Update()
        {
            //Graphics.SetRenderTarget(volumetricsRT);
            //GL.Clear(false, true, Color.clear);

            //volumetricsCam.Render();
        }
    }
}
