using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using B_Post;


namespace B_Post.Effect
{

    public enum BlurEnumMode
    {
        None,
        GaussianBlur,  
        BoxBlur,
        KawaseBlur,
        DualKawaseBlur
    }


    // 自定义组件
    [VolumeComponentMenu("B-Post-processing/Blur")]

    public class Blur : B_PostProcessing
    {
        [Tooltip("设置模糊方式")]
        public ModeParameter mode = new ModeParameter(BlurEnumMode.None);


        [Range(0f, 10f), Tooltip("模糊的迭代次数")]
        public IntParameter      BlurTimes = new ClampedIntParameter(1, 1, 10);  
        [Range(0f, 10f), Tooltip("模糊半径")]
        public FloatParameter    BlurRange = new ClampedFloatParameter(0.0f, 0.0f, 10.0f);  
        [Range(0f, 10f), Tooltip("降采样次数")]
        public IntParameter RTDownSampling = new ClampedIntParameter(1, 1, 10);  



        // 创建材质制定Shader路径
        private Material mMaterial;
        private const string mShaderName = "B_Post/Blur";   

        // 设置渲染流程中的注入点
        public override BasicInjectionPoint InjectionPoint => BasicInjectionPoint.AfterPostProcess;
        public override int OrderInInjectionPoint => 10;


        // 是否启动
        public override bool IsActive() {
            // 如果模糊模式设置为 None 或者其他相关属性没有激活，则不启动
            return mode.value != BlurEnumMode.None && (IsBlurTimesActive() || IsBlurRangeActive() || IsRTDownSamplingActive());
        }

        // 判断是否开启
        private bool IsBlurTimesActive() => BlurTimes.value != 1;    
        private bool IsBlurRangeActive() => BlurRange.value != 0.0;    
        private bool IsRTDownSamplingActive() => RTDownSampling.value != 1;    


        // 配置当前后处理
        public override void Setup() 
        {
            if (mMaterial == null)
            
            mMaterial = CoreUtils.CreateEngineMaterial(mShaderName);
        }
        // 执行渲染逻辑
        public override void Render(CommandBuffer cmd, ref RenderingData renderingData, RTHandle source, RTHandle destination) 
        {

            if (mMaterial == null) return;
            SetMatData();

            // 计算降采样后的尺寸
            var scaledWidth = Mathf.Max(1, (int)(source.rt.width / RTDownSampling.value));
            var scaledHeight = Mathf.Max(1, (int)(source.rt.height / RTDownSampling.value));

            // 创建降采样的临时渲染目标
            int tempRTId1 = Shader.PropertyToID("_TempRT1");
            int tempRTId2 = Shader.PropertyToID("_TempRT2");

            cmd.GetTemporaryRT(tempRTId1,scaledWidth,scaledHeight,0,FilterMode.Trilinear,RenderTextureFormat.Default);
            cmd.GetTemporaryRT(tempRTId2,scaledWidth,scaledHeight,0,FilterMode.Trilinear,RenderTextureFormat.Default);

            cmd.Blit(source,tempRTId1);
            switch (mode.value)
            {

                case BlurEnumMode.None:
                    break;

                case BlurEnumMode.GaussianBlur:
                    ApplyBlurEffect(cmd, tempRTId1, tempRTId2, 0);    
                    break;

                case BlurEnumMode.BoxBlur:
                    ApplyBlurEffect(cmd, tempRTId1, tempRTId2, 1);    
                    break;

                case BlurEnumMode.KawaseBlur:

                    break;
                case BlurEnumMode.DualKawaseBlur:

                    break;
            }

            cmd.Blit(tempRTId1, destination);
            // 释放临时渲染目标
            cmd.ReleaseTemporaryRT(tempRTId1);
            cmd.ReleaseTemporaryRT(tempRTId2);
        }
        // 清理临时RT
        public override void Dispose(bool disposing) 
        {
            base.Dispose(disposing);
            CoreUtils.Destroy(mMaterial);       
            
        }

        private void SetMatData()
        {

            mMaterial.SetFloat("_BlurRange", BlurRange.value);             // Shader变量  和 Volume 组件属性 绑定

        }

        private void ApplyBlurEffect(CommandBuffer cmd, int tempRTId1, int tempRTId2, int passIndex) {
            for (int i = 0; i < BlurTimes.value; i++) {
                // 应用模糊效果，使用指定的Shader Pass
                cmd.Blit(tempRTId1, tempRTId2, mMaterial, passIndex);
                // 交换渲染目标，以便下一次迭代使用
                int temp = tempRTId1;
                tempRTId1 = tempRTId2;
                tempRTId2 = temp;
            }
        }

    }

    [System.Serializable]
    public sealed class ModeParameter : VolumeParameter<BlurEnumMode> 
    { 
        public ModeParameter(BlurEnumMode value, bool overrideState = false) : base(value, overrideState) { } 
    }

}