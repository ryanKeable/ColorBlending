TEXTURE2D_X(_SourceTexLowMip);
float4 _SourceTexLowMip_TexelSize;

float4 _BloomParams1; // x: scatter, y: clamp, z: threshold (linear), w: threshold knee

#define Scatter             _BloomParams1.x
#define ClampMax            _BloomParams1.y
#define Threshold           _BloomParams1.z
#define ThresholdKnee       _BloomParams1.w

half4 BloomPrefilter(float2 uv, TEXTURE2D_X_PARAM(sourceTex, sampler_LinearClamp), float4 texelSize)
{
    // anti flicker
    half4 A = SAMPLE_TEXTURE2D_X(sourceTex, sampler_LinearClamp, uv + texelSize * float2(-1.0, -1.0));
    half4 B = SAMPLE_TEXTURE2D_X(sourceTex, sampler_LinearClamp, uv + texelSize * float2(0.0, -1.0));
    half4 C = SAMPLE_TEXTURE2D_X(sourceTex, sampler_LinearClamp, uv + texelSize * float2(1.0, -1.0));
    half4 D = SAMPLE_TEXTURE2D_X(sourceTex, sampler_LinearClamp, uv + texelSize * float2(-0.5, -0.5));
    half4 E = SAMPLE_TEXTURE2D_X(sourceTex, sampler_LinearClamp, uv + texelSize * float2(0.5, -0.5));
    half4 F = SAMPLE_TEXTURE2D_X(sourceTex, sampler_LinearClamp, uv + texelSize * float2(-1.0, 0.0));
    half4 G = SAMPLE_TEXTURE2D_X(sourceTex, sampler_LinearClamp, uv);
    half4 H = SAMPLE_TEXTURE2D_X(sourceTex, sampler_LinearClamp, uv + texelSize * float2(1.0, 0.0));
    half4 I = SAMPLE_TEXTURE2D_X(sourceTex, sampler_LinearClamp, uv + texelSize * float2(-0.5, 0.5));
    half4 J = SAMPLE_TEXTURE2D_X(sourceTex, sampler_LinearClamp, uv + texelSize * float2(0.5, 0.5));
    half4 K = SAMPLE_TEXTURE2D_X(sourceTex, sampler_LinearClamp, uv + texelSize * float2(-1.0, 1.0));
    half4 L = SAMPLE_TEXTURE2D_X(sourceTex, sampler_LinearClamp, uv + texelSize * float2(0.0, 1.0));
    half4 M = SAMPLE_TEXTURE2D_X(sourceTex, sampler_LinearClamp, uv + texelSize * float2(1.0, 1.0));

    half2 div = (1.0 / 4.0) * half2(0.5, 0.125);

    half4 o = (D + E + I + J) * div.x;
    o += (A + B + G + F) * div.y;
    o += (B + C + H + G) * div.y;
    o += (F + G + L + K) * div.y;
    o += (G + H + M + L) * div.y;

    half3 color = o.xyz;

    // User controlled clamp to limit crazy high broken spec
    color = min(ClampMax, color);

    // Thresholding
    half brightness = Max3(color.r, color.g, color.b);
    half softness = clamp(brightness - Threshold + ThresholdKnee, 0.0, 2.0 * ThresholdKnee);
    softness = (softness * softness) / (4.0 * ThresholdKnee + 1e-4);
    half multiplier = max(brightness - Threshold, softness) / max(brightness, 1e-4);
    color *= multiplier;

    return float4(color, 1.0h);
}

half4 BloomBlurH(float2 uv, TEXTURE2D_X_PARAM(sourceTex, sampler_LinearClamp), float4 _texelSize)
{
    float texelSize = _texelSize.x * 2.0;

    // 9-tap gaussian blur on the downsampled source
    half3 c0 = (SAMPLE_TEXTURE2D_X(sourceTex, sampler_LinearClamp, uv - float2(texelSize * 4.0, 0.0)));
    half3 c1 = (SAMPLE_TEXTURE2D_X(sourceTex, sampler_LinearClamp, uv - float2(texelSize * 3.0, 0.0)));
    half3 c2 = (SAMPLE_TEXTURE2D_X(sourceTex, sampler_LinearClamp, uv - float2(texelSize * 2.0, 0.0)));
    half3 c3 = (SAMPLE_TEXTURE2D_X(sourceTex, sampler_LinearClamp, uv - float2(texelSize * 1.0, 0.0)));
    half3 c4 = (SAMPLE_TEXTURE2D_X(sourceTex, sampler_LinearClamp, uv));
    half3 c5 = (SAMPLE_TEXTURE2D_X(sourceTex, sampler_LinearClamp, uv + float2(texelSize * 1.0, 0.0)));
    half3 c6 = (SAMPLE_TEXTURE2D_X(sourceTex, sampler_LinearClamp, uv + float2(texelSize * 2.0, 0.0)));
    half3 c7 = (SAMPLE_TEXTURE2D_X(sourceTex, sampler_LinearClamp, uv + float2(texelSize * 3.0, 0.0)));
    half3 c8 = (SAMPLE_TEXTURE2D_X(sourceTex, sampler_LinearClamp, uv + float2(texelSize * 4.0, 0.0)));

    half3 color = c0 * 0.01621622 + c1 * 0.05405405 + c2 * 0.12162162 + c3 * 0.19459459
    + c4 * 0.22702703
    + c5 * 0.19459459 + c6 * 0.12162162 + c7 * 0.05405405 + c8 * 0.01621622;

    return float4(color, 1.0h);
}

half4 BloomBlurV(float2 uv, TEXTURE2D_X_PARAM(sourceTex, sampler_LinearClamp), float4 _texelSize)
{
    float texelSize = _texelSize.y;

    // Optimized bilinear 5-tap gaussian on the same-sized source (9-tap equivalent)
    half3 c0 = (SAMPLE_TEXTURE2D_X(sourceTex, sampler_LinearClamp, uv - float2(0.0, texelSize * 3.23076923)));
    half3 c1 = (SAMPLE_TEXTURE2D_X(sourceTex, sampler_LinearClamp, uv - float2(0.0, texelSize * 1.38461538)));
    half3 c2 = (SAMPLE_TEXTURE2D_X(sourceTex, sampler_LinearClamp, uv));
    half3 c3 = (SAMPLE_TEXTURE2D_X(sourceTex, sampler_LinearClamp, uv + float2(0.0, texelSize * 1.38461538)));
    half3 c4 = (SAMPLE_TEXTURE2D_X(sourceTex, sampler_LinearClamp, uv + float2(0.0, texelSize * 3.23076923)));

    half3 color = c0 * 0.07027027 + c1 * 0.31621622
    + c2 * 0.22702703
    + c3 * 0.31621622 + c4 * 0.07027027;

    return float4(color, 1.0h);
}

half3 Upsample(float2 uv, TEXTURE2D_X_PARAM(sourceTex, sampler_LinearClamp), int blend)
{
    half3 highMip = (SAMPLE_TEXTURE2D_X(sourceTex, sampler_LinearClamp, uv));

    half3 lowMip = (SampleTexture2DBicubic(TEXTURE2D_X_ARGS(_SourceTexLowMip, sampler_LinearClamp), uv, _SourceTexLowMip_TexelSize.zwxy, (1.0).xx, unity_StereoEyeIndex));

    return(ToneMappedColourBlend(highMip, lowMip, highMip, Scatter, blend));
}

half4 BloomUpsample(float2 uv, TEXTURE2D_X_PARAM(sourceTex, sampler_LinearClamp), int blend)
{
    return float4(Upsample(uv, TEXTURE2D_X_ARGS(sourceTex, sampler_LinearClamp), blend), 1.0h);
}