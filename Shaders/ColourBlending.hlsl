#include "ColourBlendingModes.hlsl"

half ColourBlendOpacity;
int ColourBlendType;

half GI_ColourBlendOpacity;
int GI_ColourBlendType;

half4 ColourBlend(half4 BaseColor, half4 BlendColor, half4 InterpolationColor, float Opacity, int BlendType, half mask = 1)
{

    half4 BlendResult;

    switch(BlendType)
    {
        case 0:
        BlendResult = BlendColor; // standard linear interpolation
        break;
        
        case 1:
        BlendResult = Burn_half4(BaseColor, BlendColor);
        break;
        
        case 2:
        BlendResult = Darken_half4(BaseColor, BlendColor);
        break;
        
        case 3:
        BlendResult = Difference_half4(BaseColor, BlendColor);
        break;

        case 4:
        BlendResult = Dodge_half4(BaseColor, BlendColor);
        break;

        case 5:
        BlendResult = Divide_half4(BaseColor, BlendColor);
        break;

        case 6:
        BlendResult = Exclusion_half4(BaseColor, BlendColor);
        break;

        case 7:
        BlendResult = HardLight_half4(BaseColor, BlendColor);
        break;

        case 8:
        BlendResult = HardMix_half4(BaseColor, BlendColor);
        break;

        case 9:
        BlendResult = Lighten_half4(BaseColor, BlendColor);
        break;

        case 10:
        BlendResult = LinearBurn_half4(BaseColor, BlendColor);
        break;

        case 11:
        BlendResult = LinearDodge_half4(BaseColor, BlendColor);
        break;

        case 12:
        BlendResult = LinearLight_half4(BaseColor, BlendColor);
        break;

        case 13:
        BlendResult = LinearLightAddSub_half4(BaseColor, BlendColor);
        break;

        case 14:
        BlendResult = Multiply_half4(BaseColor, BlendColor);
        break;

        case 15:
        BlendResult = Negation_half4(BaseColor, BlendColor);
        break;
        
        case 16:
        BlendResult = Overlay_half4(BaseColor, BlendColor);
        break;

        case 17:
        BlendResult = PinLight_half4(BaseColor, BlendColor);
        break;

        case 18:
        BlendResult = Screen_half4(BaseColor, BlendColor);
        break;

        case 19:
        BlendResult = SoftLight_half4(BaseColor, BlendColor);
        break;

        case 20:
        BlendResult = Subtract_half4(BaseColor, BlendColor);
        break;

        case 21:
        BlendResult = VividLight_half4(BaseColor, BlendColor);
        break;
    }
    
    // Result *= mask;
    half4 Result = lerp(InterpolationColor, BlendResult, Opacity);
    Result = max(0, Result);
    Result = min(Result, 1000);

    return Result;
}

half3 ColourBlend(half3 BaseColor, half3 BlendColor, half3 InterpolationColor, float Opacity, int BlendType, half mask = 1)
{
    return ColourBlend(half4(BaseColor, 1.0h), half4(BlendColor, 1.0h), half4(InterpolationColor, 1.0h), Opacity, BlendType, mask).xyz;
}