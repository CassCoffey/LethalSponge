Shader "FullScreen/SpongePosterize"
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
    float4 _SpongeCameraColorBuffer_TexelSize;

    // Based on https://www.chilliant.com/rgb2hsv.html
    float3 HUEtoRGB(in float H)
    {
        float R = abs(H * 6 - 3) - 1;
        float G = 2 - abs(H * 6 - 2);
        float B = 2 - abs(H * 6 - 4);
        return saturate(float3(R,G,B));
    }

    float Epsilon = 1e-10;
 
    float3 RGBtoHCV(in float3 RGB)
    {
        // Based on work by Sam Hocevar and Emil Persson
        float4 P = (RGB.g < RGB.b) ? float4(RGB.bg, -1.0, 2.0/3.0) : float4(RGB.gb, 0.0, -1.0/3.0);
        float4 Q = (RGB.r < P.x) ? float4(P.xyw, RGB.r) : float4(RGB.r, P.yzx);
        float C = Q.x - min(Q.w, Q.y);
        float H = abs((Q.w - Q.y) / (6 * C + Epsilon) + Q.z);
        return float3(H, C, Q.x);
    }

    float3 RGBtoHSL(in float3 RGB)
    {
        float3 HCV = RGBtoHCV(RGB);
        float L = HCV.z - HCV.y * 0.5;
        float S = 0;
        float d = 1 - abs(L * 2 - 1) + Epsilon;
        if (d == 0) {
            S = 0;
        } else {
            S = HCV.y / d;
        }
        
        return float3(HCV.x, S, L);
    }

    float3 HSLtoRGB(in float3 HSL)
    {
        float3 RGB = HUEtoRGB(HSL.x);
        float C = (1 - abs(2 * HSL.z - 1)) * HSL.y;
        return ((RGB - 0.5) * C + HSL.z);
    }

    void make_color_kernel(inout float3 n[8], float2 coord)
    {
	    n[0] = LOAD_TEXTURE2D_X_LOD(_SpongeCameraColorBuffer, coord + float2( -1, -1), 0);
	    n[1] = LOAD_TEXTURE2D_X_LOD(_SpongeCameraColorBuffer, coord + float2(0.0, -1), 0);
	    n[2] = LOAD_TEXTURE2D_X_LOD(_SpongeCameraColorBuffer, coord + float2(  1, -1), 0);
	    n[3] = LOAD_TEXTURE2D_X_LOD(_SpongeCameraColorBuffer, coord + float2( -1, 0.0), 0);
	    n[4] = LOAD_TEXTURE2D_X_LOD(_SpongeCameraColorBuffer, coord + float2(  1, 0.0), 0);
	    n[5] = LOAD_TEXTURE2D_X_LOD(_SpongeCameraColorBuffer, coord + float2( -1, 1), 0);
	    n[6] = LOAD_TEXTURE2D_X_LOD(_SpongeCameraColorBuffer, coord + float2(0.0, 1), 0);
	    n[7] = LOAD_TEXTURE2D_X_LOD(_SpongeCameraColorBuffer, coord + float2(  1, 1), 0);
    }

    void make_depth_kernel(inout float n[8], float2 coord)
    {
	    n[0] = LoadCameraDepth(coord + float2( -1, -1));
	    n[1] = LoadCameraDepth(coord + float2(0.0, -1));
	    n[2] = LoadCameraDepth(coord + float2(  1, -1));
	    n[3] = LoadCameraDepth(coord + float2( -1, 0.0));
	    n[4] = LoadCameraDepth(coord + float2(  1, 0.0));
	    n[5] = LoadCameraDepth(coord + float2( -1, 1));
	    n[6] = LoadCameraDepth(coord + float2(0.0, 1));
	    n[7] = LoadCameraDepth(coord + float2(  1, 1));
    }

    float4 FullScreenReadPosterize(Varyings varyings) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(varyings);

        float2 uv = varyings.positionCS.xy;
        float4 color = LOAD_TEXTURE2D_X_LOD(_SpongeCameraColorBuffer, uv, 0);

        // Sobel Filter First
        float3 c[8];
	    make_color_kernel( c, uv );

        float d[8];
	    make_depth_kernel( d, uv );

        float3 sobel_edge_h = ((1.0*c[2]) + (2.0*c[4]) + (1.0*c[7])) - ((1.0*c[0]) + (2.0*c[3]) + (1.0*c[5]));
  	    float3 sobel_edge_v = ((1.0*c[0]) + (2.0*c[1]) + (1.0*c[2])) - ((1.0*c[5]) + (2.0*c[6]) + (1.0*c[7]));
	    float3 sobel_color = sqrt((sobel_edge_h * sobel_edge_h) + (sobel_edge_v * sobel_edge_v)) * 2;

        float sobel_float_color = clamp(max(sobel_color.r, max(sobel_color.g, sobel_color.b)), 0, 0.75);

        if (min(sobel_color.r, min(sobel_color.g, sobel_color.b)) < 0.025 || sobel_float_color < 0.3) {
            sobel_float_color = 0;
        }

        sobel_edge_h = d[2] + (2.0*d[4]) + d[7] - (d[0] + (2.0*d[3]) + d[5]);
  	    sobel_edge_v = d[0] + (2.0*d[1]) + d[2] - (d[5] + (2.0*d[6]) + d[7]);
	    float sobel_depth = sqrt((sobel_edge_h * sobel_edge_h) + (sobel_edge_v * sobel_edge_v)) * 2;
        sobel_depth = clamp(sobel_depth, 0, 1);
        float3 sobel_depth_three = float3(sobel_depth, sobel_depth, sobel_depth);
        
        // Old posterization values
        //float lowSteps = 20;
        //float lowMinimum = (1 / lowSteps) * 1;
        //float lowMaximum = (1 / lowSteps) * 2;

        //float lowSteps = 15;
        //float lowMinimum = (1 / lowSteps) * 1;
        //float lowMaximum = (1 / lowSteps) * 2;

        // Then a posterization effect with some smoothing
        // There are two specific bands we want to capture
        float lowSteps = 200;
        float lowMinimum = (1 / lowSteps) * 1;
        float lowMaximum = (1 / lowSteps) * 2;

        float highSteps = 20;
        float highMinimum = (1 / highSteps) * 1;
        float highMaximum = (1 / highSteps) * 2;

        float3 posterizeRgb = float3(color.rgb);

        // Convert to HSL
        float3 hsl = RGBtoHSL(color.rgb);
        
        float value = hsl.z;
        
        if (value >= lowMinimum && value <= lowMaximum) 
        {
            float lower     = floor(value * lowSteps) / lowSteps;
            float lowerDiff = abs(value - lower);
            float upper     = ceil(value * lowSteps) / lowSteps;
            float upperDiff = abs(upper - value);
            float level      = lowerDiff <= upperDiff ? lower : upper;
            float adjustment = level / value;
        
            hsl = float3(hsl.xy, hsl.z * adjustment);
        }

        if (value >= highMinimum && value <= highMaximum) 
        {
            float lower     = floor(value * highSteps) / highSteps;
            float lowerDiff = abs(value - lower);
            float upper     = ceil(value * highSteps) / highSteps;
            float upperDiff = abs(upper - value);
            float level      = lowerDiff <= upperDiff ? lower : upper;
            float adjustment = level / value;
        
            hsl = float3(hsl.xy, hsl.z * adjustment);
        }
        
        // Convert back to RGB
        posterizeRgb = HSLtoRGB(hsl);

        return float4(posterizeRgb.rgb * (1 - sobel_float_color) - sobel_depth_three, color.a);
    }

    TEXTURE2D_X(_PosterizationBuffer);

    float4 FullScreenWritePosterize(Varyings varyings) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(varyings);

        float2 uv = varyings.positionCS.xy;
        float4 posterize = LOAD_TEXTURE2D_X_LOD(_PosterizationBuffer, uv, 0);

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
