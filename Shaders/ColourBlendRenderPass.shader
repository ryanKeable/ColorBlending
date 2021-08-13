Shader "Rendering/ColourBlend"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" { }
        _ScreenTintTexture ("Tint (RGB)", 2D) = "white" { }
    }

    HLSLINCLUDE

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/Common.hlsl"

    #include "ColourBlending.hlsl"
    #include "ColourBlendBloom.hlsl"

    TEXTURE2D_X(_MainTex);
    float4 _MainTex_TexelSize;
    
    TEXTURE2D_X(_BloomTexture);

    TEXTURE2D_X(_ScreenTintTexture);

    int4 _BlendTypeParams;

    #define BloomUpSampleBlend      _BlendTypeParams.x
    #define BloomFinalBlend         _BlendTypeParams.y
    #define VignetteBlend           _BlendTypeParams.z
    #define ScreenTintBlend         _BlendTypeParams.w

    float _BloomFinalBlendValue;
    float _ScreenTintBlendValue;

    half4 _VignetteColor;
    float4 _VignetteParams2;

    #define VignetteCenter          _VignetteParams2.xy
    #define VignetteIntensity       _VignetteParams2.z
    #define VignetteSmoothness      _VignetteParams2.w
    
    float4 _BloomParams2;

    #define BloomColor              _BloomParams2.xyz
    #define BloomIntensity          _BloomParams2.w

    half3 _ScreenTintColor;


    half4 FragCopy(Varyings input): SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
        half4 color = SAMPLE_TEXTURE2D_X(_MainTex, sampler_LinearClamp, UnityStereoTransformScreenSpaceTex(input.uv));

        return color;
    }

    half4 FragPrefilter(Varyings input): SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        return BloomPrefilter(UnityStereoTransformScreenSpaceTex(input.uv), TEXTURE2D_X_ARGS(_MainTex, sampler_LinearClamp), _MainTex_TexelSize);
    }

    half4 FragBlurH(Varyings input): SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        return BloomBlurH(UnityStereoTransformScreenSpaceTex(input.uv), TEXTURE2D_X_ARGS(_MainTex, sampler_LinearClamp), _MainTex_TexelSize);
    }

    half4 FragBlurV(Varyings input): SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        return BloomBlurV(UnityStereoTransformScreenSpaceTex(input.uv), TEXTURE2D_X_ARGS(_MainTex, sampler_LinearClamp), _MainTex_TexelSize);
    }

    half4 FragUpsample(Varyings input): SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        return BloomUpsample(UnityStereoTransformScreenSpaceTex(input.uv), TEXTURE2D_X_ARGS(_MainTex, sampler_LinearClamp), BloomUpSampleBlend);
    }

    half3 ApplyBlendedVignette(half3 input, float2 uv, float2 center, float intensity, int blendType, float smoothness, half3 blendColor)
    {
        float roundness = 1; // TODO: make this better later
        center = UnityStereoTransformScreenSpaceTex(center);
        float2 dist = abs(uv - center) * intensity;

        #if defined(UNITY_SINGLE_PASS_STEREO)
            dist.x /= unity_StereoScaleOffset[unity_StereoEyeIndex].x;
        #endif

        dist.x *= roundness;
        float vfactor = 1 - saturate(pow(saturate(1.0 - dot(dist, dist)), smoothness));

        return ColourBlend(input, blendColor, input, vfactor, blendType);
    }
    

    half4 ColorBlendComposition(Varyings i): SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
        float2 uv = UnityStereoTransformScreenSpaceTex(i.uv);
        half3 color = SAMPLE_TEXTURE2D_X(_MainTex, sampler_LinearClamp, uv);

        #if defined(_BLOOM)
            {
                half3 bloom = SAMPLE_TEXTURE2D_X(_BloomTexture, sampler_LinearClamp, uv).xyz * BloomColor * BloomIntensity;
                color = ToneMappedColourBlend(color, bloom, color + bloom, _BloomFinalBlendValue, BloomFinalBlend);
            }
        #endif

        #if defined(_VIGNETTE)
            {
                color = ApplyBlendedVignette(color, i.uv, VignetteCenter, VignetteIntensity, VignetteBlend, VignetteSmoothness, _VignetteColor);
            }
        #endif
        
        #if defined(_TINT)
            {
                half3 blendValue = _ScreenTintBlendValue * SAMPLE_TEXTURE2D_X(_ScreenTintTexture, sampler_LinearClamp, uv).xyz;
                color = ColourBlend(color, _ScreenTintColor, color, blendValue, ScreenTintBlend);
            }
        #endif

        return float4(color, 1);
    }


    ENDHLSL

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            Name "ColorBlend Copy"

            HLSLPROGRAM

            #pragma vertex FullscreenVert
            #pragma fragment FragCopy
            ENDHLSL

        }

        Pass
        {
            Name "Bloom Prefilter"

            HLSLPROGRAM

            #pragma vertex FullscreenVert
            #pragma fragment FragPrefilter
            ENDHLSL

        }

        Pass
        {
            Name "Bloom Blur Horizontal"

            HLSLPROGRAM

            #pragma vertex FullscreenVert
            #pragma fragment FragBlurH
            ENDHLSL

        }

        Pass
        {
            Name "Bloom Blur Vertical"

            HLSLPROGRAM

            #pragma vertex FullscreenVert
            #pragma fragment FragBlurV
            ENDHLSL

        }

        Pass
        {
            Name "Bloom Upsample"

            HLSLPROGRAM

            #pragma vertex FullscreenVert
            #pragma fragment FragUpsample
            ENDHLSL

        }

        Pass
        {
            Name "ColorBlend Composition"

            HLSLPROGRAM

            #pragma vertex FullscreenVert
            #pragma fragment ColorBlendComposition

            #pragma multi_compile_local_fragment _ _BLOOM
            #pragma multi_compile_local_fragment _ _VIGNETTE
            #pragma multi_compile_local_fragment _ _TINT

            ENDHLSL

        }
    }
}