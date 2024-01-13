using B_Post.Effect;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [VolumeComponentEditor(typeof(VolumetricLight))]
    sealed class VolumLightEditor : VolumeComponentEditor
    {

        SerializedDataParameter m_ColorChange;
        SerializedDataParameter m_LightIntensity;
        SerializedDataParameter m_StepSize;
        SerializedDataParameter m_MaxDistance;
        SerializedDataParameter m_MaxStep;
        SerializedDataParameter m_Loop;
        SerializedDataParameter m_Mode;
        SerializedDataParameter m_BlurInt;
        SerializedDataParameter m_Space_S;
        SerializedDataParameter m_Space_R;
        SerializedDataParameter m_KernelSize;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<VolumetricLight>(serializedObject);


            m_ColorChange = Unpack(o.Find(x => x.ColorChange));
            m_LightIntensity = Unpack(o.Find(x => x.lightIntensity));
            m_StepSize = Unpack(o.Find(x => x.stepSize));
            m_MaxDistance = Unpack(o.Find(x => x.maxDistance));
            m_MaxStep = Unpack(o.Find(x => x.maxStep));
            m_Loop = Unpack(o.Find(x => x.loop));
            m_Mode = Unpack(o.Find(x => x.mode));
            m_BlurInt = Unpack(o.Find(x => x.BlurInt));
            m_Space_S = Unpack(o.Find(x => x.Space_S));
            m_Space_R = Unpack(o.Find(x => x.Space_R));
            m_KernelSize = Unpack(o.Find(x => x.KernelSize));
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("体积光", EditorStyles.largeLabel);


            PropertyField(m_ColorChange);
            PropertyField(m_LightIntensity);
            PropertyField(m_StepSize);
            PropertyField(m_MaxDistance);
            PropertyField(m_MaxStep);

            PropertyField(m_Mode);

            BlurMode mode = (BlurMode)m_Mode.value.enumValueIndex;
            if (mode == BlurMode.GaussianBlur)
            {
                EditorGUILayout.LabelField("高斯模糊", EditorStyles.boldLabel);
                PropertyField(m_Loop);
                PropertyField(m_BlurInt);
            }
            else if (mode == BlurMode.BilateralFilter)
            {
                EditorGUILayout.LabelField("双边滤波", EditorStyles.boldLabel);
                PropertyField(m_Loop);
                PropertyField(m_Space_S);
                PropertyField(m_Space_R);
                PropertyField(m_KernelSize);
            }
        }
    }
}
