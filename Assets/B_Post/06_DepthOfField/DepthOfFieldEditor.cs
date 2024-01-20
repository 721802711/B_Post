using B_Post.Effect;
using UnityEditor;
using UnityEngine;
//using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{

    [CustomEditor(typeof(DepthOfField))]
    sealed class DepthOfFieldEditor : VolumeComponentEditor
    {

        SerializedDataParameter m_Mode;
        SerializedDataParameter m_DepthOnly;


        SerializedDataParameter m_BlurTimes;
        SerializedDataParameter m_BlurRange;
        SerializedDataParameter m_RTDownSampling;

        SerializedDataParameter m_FocusPower;
        SerializedDataParameter m_DOFDistance;
        SerializedDataParameter m_FarBlurScale;
        SerializedDataParameter m_FarBlurScalePower;

        SerializedDataParameter m_Iteration;
        SerializedDataParameter m_DownSample;

        SerializedDataParameter m_End;
        SerializedDataParameter m_Start;
        SerializedDataParameter m_Density;



        public override void OnEnable()
        {
            var o = new PropertyFetcher<DepthOfField>(serializedObject);

            m_Mode = Unpack(o.Find(x => x.mode));
            m_DepthOnly = Unpack(o.Find(x => x.DepthOnly));

            m_BlurTimes = Unpack(o.Find(x => x.BlurTimes));
            m_BlurRange = Unpack(o.Find(x => x.BlurRange));
            m_RTDownSampling = Unpack(o.Find(x => x.RTDownSampling));

            m_FocusPower = Unpack(o.Find(x => x.FocusPower));
            m_DOFDistance = Unpack(o.Find(x => x.DOFDistance));
            m_FarBlurScale = Unpack(o.Find(x => x.FarBlurScale));
            m_FarBlurScalePower = Unpack(o.Find(x => x.FarBlurScalePower));

            m_Iteration = Unpack(o.Find(x => x.Iteration));
            m_DownSample = Unpack(o.Find(x => x.DownSample));


            m_End = Unpack(o.Find(x => x.End));
            m_Start = Unpack(o.Find(x => x.Start));
            m_Density = Unpack(o.Find(x => x.Density));

        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("DOF", EditorStyles.largeLabel);
            
            PropertyField(m_Mode);

            DepthMode mode = (DepthMode)m_Mode.value.enumValueIndex;
            if (mode == DepthMode.GaussianDOF)
            {
                EditorGUILayout.LabelField("深度开关", EditorStyles.boldLabel);
                PropertyField(m_DepthOnly);

                
                EditorGUILayout.LabelField("景深模糊", EditorStyles.boldLabel);

                PropertyField(m_FocusPower);
                PropertyField(m_DOFDistance);
                PropertyField(m_FarBlurScale);
                PropertyField(m_FarBlurScalePower);

                EditorGUILayout.LabelField("模糊", EditorStyles.boldLabel);

                PropertyField(m_BlurTimes);
                PropertyField(m_BlurRange);
                PropertyField(m_RTDownSampling);
            }
            else if (mode == DepthMode.Bokeh)
            {
                EditorGUILayout.LabelField("散景模糊", EditorStyles.boldLabel);

                PropertyField(m_BlurRange);
                PropertyField(m_Iteration);
                PropertyField(m_DownSample);

                PropertyField(m_End);
                PropertyField(m_Start);
                PropertyField(m_Density);
            }
        }
    }
}
