using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Runtime.CompilerServices;
using UnityEngine.Experimental.Rendering;
using System;

namespace UnityEditor.Rendering.Universal
{
    [Serializable, VolumeComponentMenu("Custom-Post-Processing/Color-Blend")]
    public sealed class ColorBlendSettings : VolumeComponent, IPostProcessComponent
    {
        public bool IsEnabled = true;
        public RenderPassEvent WhenToInsert = RenderPassEvent.AfterRenderingTransparents;
        public Material MaterialToBlit;

        public ColorParameter screenTint = new ColorParameter(Color.white, false, false, true);
        public ColourBlendTypeParameter screenTintBlend = new ColourBlendTypeParameter(ColorBlendType.Normal);
        public ClampedFloatParameter screenTintBlendValue = new ClampedFloatParameter(0.5f, 0f, 1f);

        public MinFloatParameter bloomThreshold = new MinFloatParameter(0.9f, 0f);
        public MinFloatParameter bloomIntenisty = new MinFloatParameter(0f, 0f);
        public ClampedFloatParameter bloomScatter = new ClampedFloatParameter(0.7f, 0f, 1f);
        public ColorParameter bloomTint = new ColorParameter(Color.white, false, false, true);
        public ColourBlendTypeParameter bloomUpSampleBlend = new ColourBlendTypeParameter(ColorBlendType.Normal);
        public ClampedFloatParameter bloomBlendValue = new ClampedFloatParameter(.5f, -1f, 1f);
        public ColourBlendTypeParameter bloomFinalBlend = new ColourBlendTypeParameter(ColorBlendType.Normal);


        public Vector2Parameter vignetteCenter = new Vector2Parameter(new Vector2(0.5f, 0.5f));
        public ClampedFloatParameter vignetteIntensity = new ClampedFloatParameter(0f, 0f, 1f);
        public ClampedFloatParameter vignetteSmoothness = new ClampedFloatParameter(0.2f, 0.01f, 1f);
        public ColorParameter vignetteTint = new ColorParameter(Color.black, false, false, true);
        public ColourBlendTypeParameter vignetteBlend = new ColourBlendTypeParameter(ColorBlendType.Normal);
        public ClampedFloatParameter vignetteBlendValue = new ClampedFloatParameter(0.5f, 0f, 1f);


        public bool IsActive() => IsEnabled;

        public bool IsTileCompatible() => false;
    }

    [Serializable]
    public sealed class ColourBlendTypeParameter : VolumeParameter<ColorBlendType> { public ColourBlendTypeParameter(ColorBlendType value, bool overrideState = false) : base(value, overrideState) { } }

    [Serializable]
    public enum ColorBlendType
    {
        Normal = 0,
        Burn = 1,
        Darken = 2,
        Difference = 3,
        Dodge = 4,
        Divivde = 5,
        Exclusion = 6,
        HardLight = 7,
        HardMix = 8,
        Lighten = 9,
        LinearBurn = 10,
        LinearDodge = 11,
        LinearLight = 12,
        LinearLightAddSub = 13,
        Multiply = 14,
        Negation = 15,
        Overlay = 16,
        PinLight = 17,
        Screen = 18,
        SoftLight = 19,
        Subtract = 20,
        VividLight = 21
    }
}