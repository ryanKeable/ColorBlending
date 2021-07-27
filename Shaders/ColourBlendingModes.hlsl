half4 Burn_half4(half4 Base, half4 Blend)
{
    return 1.0 - (1.0 - Blend) / Base;
}

half4 Darken_half4(half4 Base, half4 Blend)
{
    return min(Blend, Base);
}

half4 Difference_half4(half4 Base, half4 Blend)
{
    return abs(Blend - Base);
}

half4 Dodge_half4(half4 Base, half4 Blend)
{
    return Base / (1.0 - Blend);
}

half4 Divide_half4(half4 Base, half4 Blend)
{
    return Base / (Blend + 0.000000000001);
}

half4 Exclusion_half4(half4 Base, half4 Blend)
{
    return Blend + Base - (2.0 * Blend * Base);
}

half4 HardLight_half4(half4 Base, half4 Blend)
{
    half4 result1 = 1.0 - 2.0 * (1.0 - Base) * (1.0 - Blend);
    half4 result2 = 2.0 * Base * Blend;
    half4 zeroOrOne = step(Blend, 0.5);
    return result2 * zeroOrOne + (1 - zeroOrOne) * result1;
}

half4 HardMix_half4(half4 Base, half4 Blend)
{
    return step(1 - Base, Blend);
}

half4 Lighten_half4(half4 Base, half4 Blend)
{
    return max(Blend, Base);
}

half4 LinearBurn_half4(half4 Base, half4 Blend)
{
    return Base + Blend - 1.0;
}

half4 LinearDodge_half4(half4 Base, half4 Blend)
{
    return Base + Blend;
}

half4 LinearLight_half4(half4 Base, half4 Blend)
{
    return Blend < 0.5 ? max(Base + (2 * Blend) - 1, 0): min(Base + 2 * (Blend - 0.5), 1);
}

half4 LinearLightAddSub_half4(half4 Base, half4 Blend)
{
    return Blend + 2.0 * Base - 1.0;
}

half4 Multiply_half4(half4 Base, half4 Blend)
{
    return Base * Blend;
}

half4 Negation_half4(half4 Base, half4 Blend)
{
    return 1.0 - abs(1.0 - Blend - Base);
}

half4 Overlay_half4(half4 Base, half4 Blend)
{
    half4 result1 = 1.0 - 2.0 * (1.0 - Base) * (1.0 - Blend);
    half4 result2 = 2.0 * Base * Blend;
    half4 zeroOrOne = step(Base, 0.5);
    return result2 * zeroOrOne + (1 - zeroOrOne) * result1;
}

half4 PinLight_half4(half4 Base, half4 Blend)
{
    half4 check = step(0.5, Blend);
    half4 result1 = check * max(2.0 * (Base - 0.5), Blend);
    return result1 + (1.0 - check) * min(2.0 * Base, Blend);
}

half4 Screen_half4(half4 Base, half4 Blend)
{
    return 1.0 - (1.0 - Blend) * (1.0 - Base);
}

half4 SoftLight_half4(half4 Base, half4 Blend)
{
    half4 result1 = 2.0 * Base * Blend + Base * Base * (1.0 - 2.0 * Blend);
    half4 result2 = sqrt(Base) * (2.0 * Blend - 1.0) + 2.0 * Base * (1.0 - Blend);
    half4 zeroOrOne = step(0.5, Blend);
    return result2 * zeroOrOne + (1 - zeroOrOne) * result1;
}

half4 Subtract_half4(half4 Base, half4 Blend)
{
    return Base - Blend;
}

half4 VividLight_half4(half4 Base, half4 Blend)
{
    half4 result1 = 1.0 - (1.0 - Blend) / (2.0 * Base);
    half4 result2 = Blend / (2.0 * (1.0 - Base));
    half4 zeroOrOne = step(0.5, Base);
    return result2 * zeroOrOne + (1 - zeroOrOne) * result1;
}