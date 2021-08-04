using System.Linq;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;

namespace UnityEditor.Rendering.Universal
{
    [VolumeComponentEditor(typeof(ColorBlendSettings))]
    sealed class ColorBlendSettingsEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_ScreenTint;
        SerializedDataParameter m_ScreenTintBlend;
        SerializedDataParameter m_ScreenTintBlendValue;

        SerializedDataParameter m_BloomThreshold;
        SerializedDataParameter m_BloomIntensity;
        SerializedDataParameter m_BloomScatter;
        SerializedDataParameter m_BloomTint;
        SerializedDataParameter m_BloomUpSampleBlend;
        SerializedDataParameter m_BloomBlendValue;
        SerializedDataParameter m_BloomFinalBlend;

        SerializedDataParameter m_VignetteCenter;
        SerializedDataParameter m_VignetteIntensity;
        SerializedDataParameter m_VignetteSmoothness;
        SerializedDataParameter m_VignetteTint;
        SerializedDataParameter m_VignetteBlend;
        SerializedDataParameter m_VignetteBlendValue;


        public override void OnEnable()
        {
            var o = new PropertyFetcher<ColorBlendSettings>(serializedObject);

            m_ScreenTint = Unpack(o.Find(x => x.screenTint));
            m_ScreenTintBlend = Unpack(o.Find(x => x.screenTintBlend));
            m_ScreenTintBlendValue = Unpack(o.Find(x => x.screenTintBlendValue));

            m_BloomThreshold = Unpack(o.Find(x => x.bloomThreshold));
            m_BloomIntensity = Unpack(o.Find(x => x.bloomIntenisty));
            m_BloomScatter = Unpack(o.Find(x => x.bloomScatter));
            m_BloomTint = Unpack(o.Find(x => x.bloomTint));
            m_BloomUpSampleBlend = Unpack(o.Find(x => x.bloomUpSampleBlend));
            m_BloomBlendValue = Unpack(o.Find(x => x.bloomBlendValue));
            m_BloomFinalBlend = Unpack(o.Find(x => x.bloomFinalBlend));

            m_VignetteCenter = Unpack(o.Find(x => x.vignetteCenter));
            m_VignetteIntensity = Unpack(o.Find(x => x.vignetteIntensity));
            m_VignetteSmoothness = Unpack(o.Find(x => x.vignetteSmoothness));
            m_VignetteTint = Unpack(o.Find(x => x.vignetteTint));
            m_VignetteBlend = Unpack(o.Find(x => x.vignetteBlend));
            m_VignetteBlendValue = Unpack(o.Find(x => x.vignetteBlendValue));
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("ScreenTint", EditorStyles.miniLabel);

            PropertyField(m_ScreenTint);
            PropertyField(m_ScreenTintBlend);
            PropertyField(m_ScreenTintBlendValue);

            EditorGUILayout.LabelField("Bloom", EditorStyles.miniLabel);

            PropertyField(m_BloomThreshold);
            PropertyField(m_BloomIntensity);
            PropertyField(m_BloomScatter);
            PropertyField(m_BloomTint);
            PropertyField(m_BloomUpSampleBlend);
            PropertyField(m_BloomBlendValue);
            PropertyField(m_BloomFinalBlend);

            EditorGUILayout.LabelField("Vignette", EditorStyles.miniLabel);

            PropertyField(m_VignetteCenter);
            PropertyField(m_VignetteIntensity);
            PropertyField(m_VignetteSmoothness);
            PropertyField(m_VignetteTint);
            PropertyField(m_VignetteBlend);
            PropertyField(m_VignetteBlendValue);
        }
    }
}
