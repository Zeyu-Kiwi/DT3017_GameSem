#ifndef PSX_PERCEPTUAL_QUANTIZATION_INCLUDED
#define PSX_PERCEPTUAL_QUANTIZATION_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

// Quantize displayed colour spacing rather than linear scene brightness.
// Strength 1 distributes rounding across one quantization step; 0 uses nearest rounding.
void PSXPerceptualQuantize_float(float3 Color, float2 UV, float ColorLevels,
    float DitherStrength, bool UsePerceptual, float OriginalDitherRes, out float3 Out)
{
    if (!UsePerceptual)
    {
        // Preserve the original graph's linear quantization and 4x4 Bayer pattern.
        static const float thresholds[16] =
        {
            1.0/17.0, 9.0/17.0, 3.0/17.0, 11.0/17.0,
            13.0/17.0, 5.0/17.0, 15.0/17.0, 7.0/17.0,
            4.0/17.0, 12.0/17.0, 2.0/17.0, 10.0/17.0,
            16.0/17.0, 8.0/17.0, 14.0/17.0, 6.0/17.0
        };
        uint2 pixel = (uint2)(UV * _ScreenParams.xy);
        uint index = (pixel.x % 4) * 4 + pixel.y % 4;
        float resolution = max(ColorLevels, 1.0);
        float3 dithered = Color + max(OriginalDitherRes, 0.0) * (Color - thresholds[index]);
        Out = floor(dithered * resolution + 0.5) / resolution;
        return;
    }
    float levels = max(floor(ColorLevels + 0.5) - 1.0, 1.0);
    float3 positiveColor = max(Color, 0.0);
    float3 boundedColor = saturate(positiveColor);
#if defined(UNITY_COLORSPACE_GAMMA)
    float3 perceptual = boundedColor;
#else
    float3 perceptual = LinearToSRGB(boundedColor);
#endif

    // Stable screen-pixel hash avoids repeating Bayer tiles and temporal shimmer.
    float2 pixel = floor(UV * _ScreenParams.xy);
    float3 hash = frac(float3(pixel.xyx) * 0.1031);
    hash += dot(hash, hash.yzx + 33.33);
    float noise = frac((hash.x + hash.y) * hash.z);
    float rounding = 0.5 + (noise - 0.5) * saturate(DitherStrength);
    float3 quantized = saturate(floor(perceptual * levels + rounding) / levels);
#if defined(UNITY_COLORSPACE_GAMMA)
    Out = quantized;
#else
    Out = SRGBToLinear(quantized);
#endif
    // Preserve HDR energy for downstream bloom and tonemapping.
    Out += max(positiveColor - 1.0, 0.0);
}

void PSXPerceptualQuantize_half(half3 Color, half2 UV, half ColorLevels,
    half DitherStrength, bool UsePerceptual, half OriginalDitherRes, out half3 Out)
{
    float3 result;
    PSXPerceptualQuantize_float(Color, UV, ColorLevels, DitherStrength, UsePerceptual, OriginalDitherRes, result);
    Out = result;
}
#endif