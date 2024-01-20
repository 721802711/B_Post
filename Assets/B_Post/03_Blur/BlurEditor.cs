using B_Post.Effect;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [CustomEditor(typeof(Blur))]
    sealed class BlurEditor : VolumeComponentEditor
    {

        SerializedDataParameter m_Mode;

        SerializedDataParameter m_BlurTimes;
        SerializedDataParameter m_BlurRange;
        SerializedDataParameter m_RTDownSampling;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<Blur>(serializedObject);

            m_Mode = Unpack(o.Find(x => x.mode));

            m_BlurTimes = Unpack(o.Find(x => x.BlurTimes));
            m_BlurRange = Unpack(o.Find(x => x.BlurRange));
            m_RTDownSampling = Unpack(o.Find(x => x.RTDownSampling));

        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("模糊", EditorStyles.largeLabel);
            
            PropertyField(m_Mode);



            BlurEnumMode mode = (BlurEnumMode)m_Mode.value.enumValueIndex;
            if (mode == BlurEnumMode.GaussianBlur)
            {
                EditorGUILayout.LabelField("高斯模糊", EditorStyles.boldLabel);

                PropertyField(m_BlurTimes);
                PropertyField(m_BlurRange);
                PropertyField(m_RTDownSampling);

            }
            else if (mode == BlurEnumMode.BoxBlur)
            {
                EditorGUILayout.LabelField("方框模糊", EditorStyles.boldLabel);

                PropertyField(m_BlurTimes);
                PropertyField(m_BlurRange);
                PropertyField(m_RTDownSampling);

            }
            else if (mode == BlurEnumMode.KawaseBlur)
            {
                EditorGUILayout.LabelField("Kawase模糊", EditorStyles.boldLabel);
                PropertyField(m_BlurTimes);
                PropertyField(m_BlurRange);
                PropertyField(m_RTDownSampling);

            }
            else if (mode == BlurEnumMode.DualKawaseBlur)
            {
                EditorGUILayout.LabelField("双重模糊", EditorStyles.boldLabel);
                PropertyField(m_BlurTimes);
                PropertyField(m_BlurRange);
                PropertyField(m_RTDownSampling);
            }
        }
    }
}
