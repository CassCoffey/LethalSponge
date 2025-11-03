Shader "FullScreen/SpongePosterizeLegacy"
{
    HLSLINCLUDE

    #pragma vertex Vert

    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassCommon.hlsl"

    // The PositionInputs struct allow you to retrieve a lot of useful information for your fullScreenShader:
    // struct PositionInputs
    // {
    //     float3 positionWS;  // World space position (could be camera-relative)
    //     float2 positionNDC; // Normalized screen coordinates within the viewport    : [0, 1) (with the half-pixel offset)
    //     uint2  positionSS;  // Screen space pixel coordinates                       : [0, NumPixels)
    //     uint2  tileCoord;   // Screen tile coordinates                              : [0, NumTiles)
    //     float  deviceDepth; // Depth from the depth buffer                          : [0, 1] (typically reversed)
    //     float  linearDepth; // View space Z coordinate                              : [Near, Far]
    // };

    // To sample custom buffers, you have access to these functions:
    // But be careful, on most platforms you can't sample to the bound color buffer. It means that you
    // can't use the SampleCustomColor when the pass color buffer is set to custom (and same for camera the buffer).
    // float4 CustomPassSampleCustomColor(float2 uv);
    // float4 CustomPassLoadCustomColor(uint2 pixelCoords);
    // float LoadCustomDepth(uint2 pixelCoords);
    // float SampleCustomDepth(float2 uv);

    // There are also a lot of utility function you can use inside Common.hlsl and Color.hlsl,
    // you can check them out in the source code of the core SRP package.

    TEXTURE2D_X(_SpongeCameraColorBuffer);

    void make_color_kernel(inout float3 n[9], float2 coord)
    {
	    n[0] = LOAD_TEXTURE2D_X_LOD(_SpongeCameraColorBuffer, coord + float2( -1, -1), 0);
	    n[1] = LOAD_TEXTURE2D_X_LOD(_SpongeCameraColorBuffer, coord + float2(0.0, -1), 0);
	    n[2] = LOAD_TEXTURE2D_X_LOD(_SpongeCameraColorBuffer, coord + float2(  1, -1), 0);
	    n[3] = LOAD_TEXTURE2D_X_LOD(_SpongeCameraColorBuffer, coord + float2( -1, 0.0), 0);
	    n[4] = LOAD_TEXTURE2D_X_LOD(_SpongeCameraColorBuffer, coord, 0);
	    n[5] = LOAD_TEXTURE2D_X_LOD(_SpongeCameraColorBuffer, coord + float2(  1, 0.0), 0);
	    n[6] = LOAD_TEXTURE2D_X_LOD(_SpongeCameraColorBuffer, coord + float2( -1, 1), 0);
	    n[7] = LOAD_TEXTURE2D_X_LOD(_SpongeCameraColorBuffer, coord + float2(0.0, 1), 0);
	    n[8] = LOAD_TEXTURE2D_X_LOD(_SpongeCameraColorBuffer, coord + float2(  1, 1), 0);
    }

    void make_depth_kernel(inout float n[9], float2 coord)
    {
	    n[0] = LoadCameraDepth(coord + float2( -1, -1));
	    n[1] = LoadCameraDepth(coord + float2(0.0, -1));
	    n[2] = LoadCameraDepth(coord + float2(  1, -1));
	    n[3] = LoadCameraDepth(coord + float2( -1, 0.0));
	    n[4] = LoadCameraDepth(coord);
	    n[5] = LoadCameraDepth(coord + float2(  1, 0.0));
	    n[6] = LoadCameraDepth(coord + float2( -1, 1));
	    n[7] = LoadCameraDepth(coord + float2(0.0, 1));
	    n[8] = LoadCameraDepth(coord + float2(  1, 1));
    }
    
    //sampler _CameraDepthBuffer;

    float4 FullScreenReadPosterize(Varyings varyings) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(varyings);

        float2 uv = varyings.positionCS.xy;
        float4 color = LOAD_TEXTURE2D_X_LOD(_SpongeCameraColorBuffer, uv, 0);

        // Sobel Filter First
        float3 c[9];
	    make_color_kernel( c, uv );

        float d[9];
	    make_depth_kernel( d, uv );

        float3 sobel_edge_h = c[2] + (2.0*c[5]) + c[8] - (c[0] + (2.0*c[3]) + c[6]);
  	    float3 sobel_edge_v = c[0] + (2.0*c[1]) + c[2] - (c[6] + (2.0*c[7]) + c[8]);
	    float3 sobel_color = sqrt((sobel_edge_h * sobel_edge_h) + (sobel_edge_v * sobel_edge_v)) / 5;

        sobel_edge_h = d[2] + (2.0*d[5]) + d[8] - (d[0] + (2.0*d[3]) + d[6]);
  	    sobel_edge_v = d[0] + (2.0*d[1]) + d[2] - (d[6] + (2.0*d[7]) + d[8]);
	    float sobel_depth = sqrt((sobel_edge_h * sobel_edge_h) + (sobel_edge_v * sobel_edge_v)) / 1.5;
        float3 sobel_depth3 = float3(sobel_depth, sobel_depth, sobel_depth);

        // Then a posterization effect with some smoothing
        float levels = 5.5;
        float minimum = 1 / levels;
        
        float greyscale = max(color.r, max(color.g, color.b));
        
        if (greyscale >= minimum) {
            return float4(color.rgb - sobel_color.rgb - sobel_depth3.rgb, color.a);
        }
        
        float lower     = floor(greyscale * levels) / levels;
        float lowerDiff = abs(greyscale - lower);
        float upper     = ceil(greyscale * levels) / levels;
        float upperDiff = abs(upper - greyscale);
        float level      = lowerDiff <= upperDiff ? greyscale : upper;
        float adjustment = level / greyscale;
        float3 posterizeColor = color.rgb * adjustment;

        return float4(posterizeColor.rgb - sobel_color.rgb - sobel_depth3.rgb, color.a);
    }

    TEXTURE2D_X(_PosterizationBuffer);

    float4 FullScreenWritePosterize(Varyings varyings) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(varyings);
        float depth = LoadCameraDepth(varyings.positionCS.xy);
        PositionInputs posInput = GetPositionInput(varyings.positionCS.xy, _ScreenSize.zw, depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);

        float2 uv = posInput.positionNDC.xy * _RTHandleScale.xy;
        float4 posterize = SAMPLE_TEXTURE2D_X_LOD(_PosterizationBuffer, s_linear_clamp_sampler, uv, 0);

        return float4(posterize.rgba);
    }

    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            Name "ReadColor"

            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off

            HLSLPROGRAM
                #pragma fragment FullScreenReadPosterize
            ENDHLSL
        }
        Pass
        {
            Name "WriteColor"

            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off

            HLSLPROGRAM
                #pragma fragment FullScreenWritePosterize
            ENDHLSL
        }
    }
    Fallback Off
}
