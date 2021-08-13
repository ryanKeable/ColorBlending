using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Runtime.CompilerServices;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Rendering.Universal
{
    public class ColorBlendRenderFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public class ColorBlendRenderFeatureSettings
        {
            public bool IsEnabled = true;
            public RenderPassEvent WhenToInsert = RenderPassEvent.AfterRenderingTransparents;
            public Material MaterialToBlit;
        }

        public ColorBlendRenderFeatureSettings settings = new ColorBlendRenderFeatureSettings();

        class ColorBlendRenderPass : ScriptableRenderPass
        {

            // used to label this pass in Unity's Frame Debug utility
            string _profilerTag;
            Material _materialToBlit;
            RenderTargetIdentifier _source; // _CameraColorTexture
            RenderTextureDescriptor _descriptor;

            ColorBlendSettings _ColorBlend;

            const int k_MaxPyramidSize = 16;

            // store shader params here
            static class ShaderParams
            {
                public static readonly int tempCopyString;

                public static readonly int blendTypes;
                public static readonly int blendTypeValues;

                public static readonly int screenTint;

                public static readonly int bloomParams1;
                public static readonly int bloomParams2;
                public static int[] bloomMipUp;
                public static int[] bloomMipDown;
                public static readonly int bloomTexLowMip;
                public static readonly int bloomTexture;

                public static readonly int vignetteColor;
                public static readonly int vignetteParams2;



                static ShaderParams()
                {
                    tempCopyString = Shader.PropertyToID("_TempCopy");

                    blendTypes = Shader.PropertyToID("_BlendTypeParams");
                    blendTypeValues = Shader.PropertyToID("_BlendValueParams");

                    screenTint = Shader.PropertyToID("_ScreenTintColor");

                    bloomParams1 = Shader.PropertyToID("_BloomParams1");
                    bloomParams2 = Shader.PropertyToID("_BloomParams2");
                    bloomTexture = Shader.PropertyToID("_BloomTexture");
                    bloomTexLowMip = Shader.PropertyToID("_SourceTexLowMip");

                    vignetteColor = Shader.PropertyToID("_VignetteColor");
                    vignetteParams2 = Shader.PropertyToID("_VignetteParams2");
                }
            }

            public ColorBlendRenderPass(string profilerTag, ColorBlendRenderFeatureSettings settings)
            {
                this._profilerTag = profilerTag;
                this.renderPassEvent = settings.WhenToInsert;
                this._materialToBlit = settings.MaterialToBlit;

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
            public void Setup(RenderTargetIdentifier source, RenderTextureDescriptor desc, ColorBlendSettings colorBlend)
            {
                this._source = source;
                this._descriptor = desc;
                this._ColorBlend = colorBlend;
            }

            // Here you can implement the rendering logic.
            // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
            // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
            // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                CommandBuffer cmd = CommandBufferPool.Get(_profilerTag);
                cmd.Clear();

                SetupColorBlending();
                SetupBloom(cmd);
                SetupVignette();
                SetupScreenTint();

                cmd.GetTemporaryRT(ShaderParams.tempCopyString, _descriptor, FilterMode.Bilinear);

                // copy camera color
                cmd.Blit(_source, ShaderParams.tempCopyString, _materialToBlit, 0);
                cmd.Blit(ShaderParams.tempCopyString, _source, _materialToBlit, 5);

                context.ExecuteCommandBuffer(cmd);

                cmd.Clear();
                CommandBufferPool.Release(cmd);
            }

            #region ColorBlending

            void SetupColorBlending()
            {
                int bloomUpSampleBlend = (int)_ColorBlend.bloomUpSampleBlend.value;
                int bloomFinalBlend = (int)_ColorBlend.bloomFinalBlend.value;
                int vignetteTintBlend = (int)_ColorBlend.vignetteBlend.value;
                int screenTintBlend = (int)_ColorBlend.screenTintBlend.value;

                Vector4 blendTypes = new Vector4(bloomUpSampleBlend, bloomFinalBlend, vignetteTintBlend, screenTintBlend);
                _materialToBlit.SetVector(ShaderParams.blendTypes, blendTypes);

                _materialToBlit.SetFloat("_BloomFinalBlendValue", _ColorBlend.bloomBlendValue.value);
                _materialToBlit.SetFloat("_ScreenTintBlendValue", _ColorBlend.screenTintBlendValue.value);
            }

            #endregion

            #region Bloom

            void SetupBloom(CommandBuffer cmd)
            {
                if (_ColorBlend.bloomIntenisty.value <= 0)
                {
                    _materialToBlit.DisableKeyword("_BLOOM");
                    return;
                }

                // Start at half-res
                int tw = _descriptor.width >> 1;
                int th = _descriptor.height >> 1;

                // Determine the iteration count
                int maxSize = Mathf.Max(tw, th);
                int iterations = Mathf.FloorToInt(Mathf.Log(maxSize, 2f) - 1);
                int mipCount = Mathf.Clamp(iterations, 1, k_MaxPyramidSize);

                // Pre-filtering parameters
                float clamp = 65472f;
                float threshold = Mathf.GammaToLinearSpace(_ColorBlend.bloomThreshold.value);
                float thresholdKnee = threshold * 0.5f; // Hardcoded soft knee

                // Material setup
                float scatter = Mathf.Lerp(0.05f, 0.95f, _ColorBlend.bloomScatter.value);
                _materialToBlit.SetVector(ShaderParams.bloomParams1, new Vector4(scatter, clamp, threshold, thresholdKnee));

                // Prefilter
                var desc = _descriptor;
                desc.width = tw;
                desc.height = th;
                desc.graphicsFormat = GraphicsFormat.B10G11R11_UFloatPack32;
                desc.depthBufferBits = 0;
                desc.msaaSamples = 1;

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
                var tint = _ColorBlend.bloomTint.value.linear;
                var luma = ColorUtils.Luminance(tint);
                tint = luma > 0f ? tint * (1f / luma) : Color.white;

                var bloomParams = new Vector4(tint.r, tint.g, tint.b, _ColorBlend.bloomIntenisty.value);
                Color tonemap = tint * (1 / (Mathf.Max(Mathf.Max(tint.r, tint.g), tint.b) + 1.0f));

                _materialToBlit.SetVector(ShaderParams.bloomParams2, bloomParams);
                _materialToBlit.EnableKeyword("_BLOOM");

                cmd.SetGlobalTexture(ShaderParams.bloomTexture, ShaderParams.bloomMipUp[0]);
            }

            #endregion

            #region Vignette

            void SetupVignette()
            {
                if (_ColorBlend.vignetteIntensity.value <= 0)
                {
                    _materialToBlit.DisableKeyword("_VIGNETTE");
                    return;
                }

                var color = _ColorBlend.vignetteTint.value.linear;
                var center = _ColorBlend.vignetteCenter.value;
                var aspectRatio = _descriptor.width / (float)_descriptor.height;

                var v1 = new Vector4(
                    color.r, color.g, color.b, color.a //aspectRatio or 1
                );
                var v2 = new Vector4(
                    center.x, center.y,
                    _ColorBlend.vignetteIntensity.value * 3f,
                    _ColorBlend.vignetteSmoothness.value * 5f
                );

                _materialToBlit.SetVector(ShaderParams.vignetteColor, v1);
                _materialToBlit.SetVector(ShaderParams.vignetteParams2, v2);
                _materialToBlit.EnableKeyword("_VIGNETTE");

            }

            #endregion

            #region ScreenTint

            void SetupScreenTint()
            {
                if (_ColorBlend.screenTintBlendValue.value <= 0)
                {
                    _materialToBlit.DisableKeyword("_TINT");
                    return;
                }

                _materialToBlit.SetColor("_ScreenTintColor", _ColorBlend.screenTint.value.linear);
                _materialToBlit.SetTexture("_ScreenTintTexture", _ColorBlend.screenTintTexture.value);
                _materialToBlit.EnableKeyword("_TINT");
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
                cmd.ReleaseTemporaryRT(ShaderParams.bloomMipUp[0]);
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
            ColorBlendSettings colorBlend = VolumeManager.instance.stack.GetComponent<ColorBlendSettings>();

            if (!settings.IsEnabled || settings.MaterialToBlit == null || !colorBlend.IsActive())
            {
                // we can do nothing this frame if we want
                return;
            }

            colorBlendRenderPass.Setup(renderer.cameraColorTarget, renderingData.cameraData.cameraTargetDescriptor, colorBlend);

            renderer.EnqueuePass(colorBlendRenderPass);
        }
    }
}