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
                    ApplyKawaseBlurEffect(cmd, tempRTId1, tempRTId2, 2);
                    break;
                case BlurEnumMode.DualKawaseBlur:
                    ApplyDualKawaseBlurEffect(cmd,source, tempRTId1, 3, 4);
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

        private void ApplyBlurEffect(CommandBuffer cmd, int sourceRT, int destinationRT, int passIndex) {
            for (int i = 0; i < BlurTimes.value; i++) {
                // 应用模糊效果，使用指定的Shader Pass
                cmd.Blit(sourceRT, destinationRT, mMaterial, passIndex);
                // 交换渲染目标，以便下一次迭代使用
                int temp = sourceRT;
                sourceRT = destinationRT;
                destinationRT = temp;
            }
        }

        private void ApplyKawaseBlurEffect(CommandBuffer cmd, int sourceRT, int destinationRT, int passIndex) {
            mMaterial.SetFloat("_BlurRange", BlurRange.value);
            cmd.Blit(sourceRT, destinationRT, mMaterial, passIndex); // 假设Kawase模糊的Pass索引是2

            for (int i = 1; i < BlurTimes.value; i++) 
            {
                mMaterial.SetFloat("_BlurRange", i * BlurRange.value + 1);
                cmd.Blit(destinationRT, sourceRT, mMaterial, passIndex);

                // 交换两个渲染目标
                int temp = sourceRT;
                sourceRT = destinationRT;
                destinationRT = temp;
            }

        }

        private void ApplyDualKawaseBlurEffect(CommandBuffer cmd, RTHandle source, int destinationRT, int downsamplePassIndex, int upsamplePassIndex) {
            int blurIterations = BlurTimes.value;
            int[] downSampleRT = new int[blurIterations];
            int[] upSampleRT = new int[blurIterations];



            // 向下采样
            int width = source.rt.width;
            int height = source.rt.height;
            RenderTargetIdentifier lastDownsampledRT = source;

            // 向下采样阶段
            for (int i = 0; i < blurIterations; i++) {
                downSampleRT[i] = Shader.PropertyToID("_DownSampleRT" + i);
                width = Mathf.Max(width / 2, 1);
                height = Mathf.Max(height / 2, 1);
                cmd.GetTemporaryRT(downSampleRT[i], width, height, 0, FilterMode.Bilinear, RenderTextureFormat.Default);
                cmd.Blit(lastDownsampledRT, downSampleRT[i], mMaterial, downsamplePassIndex);
                lastDownsampledRT = downSampleRT[i]; // 直接使用 RenderTargetIdentifier
            }

            // 向上采样阶段
            for (int i = blurIterations - 2; i >= 0; i--) {
                upSampleRT[i] = Shader.PropertyToID("_UpSampleRT" + i);
                cmd.GetTemporaryRT(upSampleRT[i], width, height, 0, FilterMode.Bilinear, RenderTextureFormat.Default);
                cmd.Blit(lastDownsampledRT, upSampleRT[i], mMaterial, upsamplePassIndex);
                lastDownsampledRT = upSampleRT[i]; // 直接使用 RenderTargetIdentifier
                width *= 2;
                height *= 2;
            }

            // 最后一步模糊
            cmd.Blit(lastDownsampledRT, destinationRT, mMaterial, upsamplePassIndex);

            // 释放所有临时RT
            for (int i = 0; i < blurIterations; i++) {
                cmd.ReleaseTemporaryRT(downSampleRT[i]);
                cmd.ReleaseTemporaryRT(upSampleRT[i]);
            }
        }

    }

    [System.Serializable]
    public sealed class ModeParameter : VolumeParameter<BlurEnumMode> 
    { 
        public ModeParameter(BlurEnumMode value, bool overrideState = false) : base(value, overrideState) { } 
    }

}