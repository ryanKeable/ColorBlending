using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Runtime.CompilerServices;
using UnityEngine.Experimental.Rendering;

public class ColorBlendRenderFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class ColorBlendRenderSettings
    {
        public bool IsEnabled = true;
        public RenderPassEvent WhenToInsert = RenderPassEvent.AfterRenderingTransparents;
        public Material MaterialToBlit;

        public ColorBlendType colorBlendType;
        public Color blendColor;

        public MinFloatParameter bloomThreshold = new MinFloatParameter(0.9f, 0f);
        public MinFloatParameter bloomIntenisty = new MinFloatParameter(0f, 0f);
        public ClampedFloatParameter bloomScatter = new ClampedFloatParameter(0.7f, 0f, 1f);
        public ColorParameter bloomTint = new ColorParameter(Color.white, false, false, true);
        public ColorBlendType upSampleBloomBlend;
        public ClampedFloatParameter bloomBlendValue = new ClampedFloatParameter(.5f, -1f, 1f);
        public ColorBlendType finalBloomBlend;

        public Vector2Parameter vignetteCenter = new Vector2Parameter(new Vector2(0.5f, 0.5f));
        public ClampedFloatParameter vignetteIntensity = new ClampedFloatParameter(0f, 0f, 1f);
        public ClampedFloatParameter vignetteSmoothness = new ClampedFloatParameter(0.2f, 0.01f, 1f);
        public ColorParameter vignetteTint = new ColorParameter(Color.black, false, false, true);
        public ColorBlendType vignetteBlend;

    }

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

    public ColorBlendRenderSettings settings = new ColorBlendRenderSettings();

    class ColorBlendRenderPass : ScriptableRenderPass
    {

        // used to label this pass in Unity's Frame Debug utility
        string _profilerTag;
        Material _materialToBlit;
        RenderTargetIdentifier _source; // _CameraColorTexture
        RenderTextureDescriptor _descriptor;

        ColorBlendType _colorBlendType;
        Color _blendColor;

        float _bloomThreshold;
        float _bloomIntenisty; // range me
        float _bloomScatter; // range me
        ColorParameter _bloomTint;
        ColorBlendType _upSampleBloomBlend;
        float _bloomBlendValue;
        ColorBlendType _finalBloomBlend;
        const int k_MaxPyramidSize = 16;

        Vector2Parameter _vignetteCenter;
        float _vignetteSmoothness;
        float _vignetteIntensity;
        ColorParameter _vignetteTint;
        ColorBlendType _vignetteBlend;

        // store shader params here
        static class ShaderParams
        {
            public static readonly int tempCopyString;
            public static readonly int blitTex;

            public static readonly int bloomParams1;
            public static readonly int bloomParams2;
            public static int[] bloomMipUp;
            public static int[] bloomMipDown;
            public static readonly int bloomTexLowMip;
            public static readonly int bloomTexture;

            public static readonly int vignetteParams1;
            public static readonly int vignetteParams2;
            public static readonly int vignetteBlend;


            static ShaderParams()
            {
                tempCopyString = Shader.PropertyToID("_TempCopy");
                blitTex = Shader.PropertyToID("_BlitTex");

                bloomParams1 = Shader.PropertyToID("_BloomParams1");
                bloomParams2 = Shader.PropertyToID("_BloomParams2");
                bloomTexture = Shader.PropertyToID("_BloomTexture");
                bloomTexLowMip = Shader.PropertyToID("_BloomTexLowMip");

                vignetteParams1 = Shader.PropertyToID("_VignetteParams1");
                vignetteParams2 = Shader.PropertyToID("_VignetteParams2");
                vignetteBlend = Shader.PropertyToID("_VignetteBlendType");
            }
        }

        public ColorBlendRenderPass(string profilerTag, ColorBlendRenderSettings settings)
        {
            this._profilerTag = profilerTag;
            this.renderPassEvent = settings.WhenToInsert;
            this._materialToBlit = settings.MaterialToBlit;

            this._colorBlendType = settings.colorBlendType;
            this._blendColor = settings.blendColor;

            this._bloomThreshold = settings.bloomThreshold.value;
            this._bloomIntenisty = settings.bloomIntenisty.value; // range me
            this._bloomScatter = settings.bloomScatter.value; // range me
            this._bloomTint = settings.bloomTint;
            this._upSampleBloomBlend = settings.upSampleBloomBlend;
            this._bloomBlendValue = settings.bloomBlendValue.value;
            this._finalBloomBlend = settings.finalBloomBlend;

            this._vignetteCenter = settings.vignetteCenter;
            this._vignetteSmoothness = settings.vignetteSmoothness.value;
            this._vignetteIntensity = settings.vignetteIntensity.value;
            this._vignetteTint = settings.vignetteTint;
            this._vignetteBlend = settings.vignetteBlend;

            ShaderParams.bloomMipUp = new int[k_MaxPyramidSize];
            ShaderParams.bloomMipDown = new int[k_MaxPyramidSize];

            for (int i = 0; i < k_MaxPyramidSize; i++)
            {
                ShaderParams.bloomMipUp[i] = Shader.PropertyToID("_BloomMipUp" + i);
                ShaderParams.bloomMipDown[i] = Shader.PropertyToID("_BloomMipDown" + i);
            }

        }

        // This isn't part of the ScriptableRenderPass class and is our own addition.
        // For this custom pass we need the camera's color target, so that gets passed in.
        public void Setup(RenderTargetIdentifier source, RenderTextureDescriptor desc)
        {
            this._source = source;
            this._descriptor = desc;
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(_profilerTag);
            cmd.Clear();


            SetupVignette();
            SetupBloom(cmd);

            cmd.GetTemporaryRT(ShaderParams.tempCopyString, _descriptor, FilterMode.Point);

            _materialToBlit.SetColor("_BlendColor", _blendColor);
            _materialToBlit.SetInt("_ColorBlendType", (int)_colorBlendType);

            // copy camera color
            cmd.Blit(_source, ShaderParams.tempCopyString, _materialToBlit, 0);
            cmd.Blit(ShaderParams.tempCopyString, _source, _materialToBlit, 5);

            context.ExecuteCommandBuffer(cmd);

            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        #region Bloom

        void SetupBloom(CommandBuffer cmd)
        {
            if (_bloomIntenisty <= 0) return;

            // Start at half-res
            int tw = _descriptor.width;// >> 1;
            int th = _descriptor.height;// >> 1;

            // Determine the iteration count
            int maxSize = Mathf.Max(tw, th);
            int iterations = Mathf.FloorToInt(Mathf.Log(maxSize, 2f) - 1);
            int mipCount = Mathf.Clamp(iterations, 1, k_MaxPyramidSize);

            // Pre-filtering parameters
            float clamp = 65472f;
            float threshold = Mathf.GammaToLinearSpace(_bloomThreshold);
            float thresholdKnee = threshold * 0.5f; // Hardcoded soft knee

            // Material setup
            float scatter = Mathf.Lerp(0.05f, 0.95f, _bloomScatter);
            _materialToBlit.SetVector(ShaderParams.bloomParams1, new Vector4(scatter, clamp, threshold, thresholdKnee));
            _materialToBlit.SetInt("_UpSampleBloomBlendType", (int)_upSampleBloomBlend);

            // Prefilter
            GraphicsFormat format = GraphicsFormat.B10G11R11_UFloatPack32;
            var desc = GetCompatibleDescriptor(tw, th, format);

            cmd.GetTemporaryRT(ShaderParams.bloomMipDown[0], desc, FilterMode.Bilinear);
            cmd.GetTemporaryRT(ShaderParams.bloomMipUp[0], desc, FilterMode.Bilinear);

            Blit(cmd, _source, ShaderParams.bloomMipDown[0], _materialToBlit, 1);

            // Downsample - gaussian pyramid
            int lastDown = ShaderParams.bloomMipDown[0];
            for (int i = 1; i < mipCount; i++)
            {
                tw = Mathf.Max(1, tw >> 1);
                th = Mathf.Max(1, th >> 1);
                int mipDown = ShaderParams.bloomMipDown[i];
                int mipUp = ShaderParams.bloomMipUp[i];

                desc.width = tw;
                desc.height = th;

                cmd.GetTemporaryRT(mipDown, desc, FilterMode.Bilinear);
                cmd.GetTemporaryRT(mipUp, desc, FilterMode.Bilinear);

                Blit(cmd, lastDown, mipUp, _materialToBlit, 2);
                Blit(cmd, mipUp, mipDown, _materialToBlit, 3);

                lastDown = mipDown;
            }

            // Upsample (bilinear by default, HQ filtering does bicubic instead
            for (int i = mipCount - 2; i >= 0; i--)
            {
                int lowMip = (i == mipCount - 2) ? ShaderParams.bloomMipDown[i + 1] : ShaderParams.bloomMipUp[i + 1];
                int highMip = ShaderParams.bloomMipDown[i];
                int dst = ShaderParams.bloomMipUp[i];

                cmd.SetGlobalTexture(ShaderParams.bloomTexLowMip, lowMip);
                Blit(cmd, highMip, BlitDstDiscardContent(cmd, dst), _materialToBlit, 4);
            }

            // Cleanup
            for (int i = 0; i < mipCount; i++)
            {
                cmd.ReleaseTemporaryRT(ShaderParams.bloomMipDown[i]);
                if (i > 0) cmd.ReleaseTemporaryRT(ShaderParams.bloomMipUp[i]);
            }

            // Setup bloom on uber
            var tint = _bloomTint.value.linear;
            var luma = ColorUtils.Luminance(tint);
            tint = luma > 0f ? tint * (1f / luma) : Color.white;

            var bloomParams = new Vector4(tint.r, tint.g, tint.b, _bloomIntenisty);
            _materialToBlit.SetVector(ShaderParams.bloomParams2, bloomParams);
            _materialToBlit.SetInt("_FinalBloomBlendType", (int)_finalBloomBlend);
            _materialToBlit.SetFloat("_BloomBlendValue", _bloomBlendValue);

            cmd.SetGlobalTexture(ShaderParams.bloomTexture, ShaderParams.bloomMipUp[0]);
        }

        #endregion

        #region Vignette

        void SetupVignette()
        {
            if (_vignetteIntensity <= 0 || _vignetteBlend == ColorBlendType.Normal) return;

            var color = _vignetteTint.value;
            var center = _vignetteCenter.value;
            var aspectRatio = _descriptor.width / (float)_descriptor.height;

            var v1 = new Vector4(
                color.r, color.g, color.b, color.a //aspectRatio or 1
            );
            var v2 = new Vector4(
                center.x, center.y,
                _vignetteIntensity * 3f,
                _vignetteSmoothness * 5f
            );

            _materialToBlit.SetVector(ShaderParams.vignetteParams1, v1);
            _materialToBlit.SetVector(ShaderParams.vignetteParams2, v2);
            _materialToBlit.SetInt(ShaderParams.vignetteBlend, (int)_vignetteBlend);

        }

        #endregion

        #region Helpers

        RenderTextureDescriptor GetCompatibleDescriptor()
            => GetCompatibleDescriptor(_descriptor.width, _descriptor.height, _descriptor.graphicsFormat, _descriptor.depthBufferBits);

        RenderTextureDescriptor GetCompatibleDescriptor(int width, int height, GraphicsFormat format, int depthBufferBits = 0)
        {
            var desc = _descriptor;
            desc.depthBufferBits = depthBufferBits;
            desc.msaaSamples = 1;
            desc.width = width;
            desc.height = height;
            desc.graphicsFormat = format;
            return desc;
        }

        private BuiltinRenderTextureType BlitDstDiscardContent(CommandBuffer cmd, RenderTargetIdentifier rt)
        {
            // We set depth to DontCare because rt might be the source of PostProcessing used as a temporary target
            // Source typically comes with a depth buffer and right now we don't have a way to only bind the color attachment of a RenderTargetIdentifier
            cmd.SetRenderTarget(new RenderTargetIdentifier(rt, 0, CubemapFace.Unknown, -1),
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare);
            return BuiltinRenderTextureType.CurrentActive;
        }

        /// Cleanup any allocated resources that were created during the execution of this render pass.
        public override void FrameCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(ShaderParams.tempCopyString);
        }

        #endregion
    }

    ColorBlendRenderPass colorBlendRenderPass;

    public override void Create()
    {
        // might be worth while passing through settings as an object and disecting inside the forward function
        colorBlendRenderPass = new ColorBlendRenderPass("ColorBlend RenderPass", settings);
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (!settings.IsEnabled || settings.MaterialToBlit == null)
        {
            // we can do nothing this frame if we want
            return;
        }

        colorBlendRenderPass.Setup(renderer.cameraColorTarget, renderingData.cameraData.cameraTargetDescriptor);

        renderer.EnqueuePass(colorBlendRenderPass);
    }
}


