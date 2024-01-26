using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using B_Post;


namespace B_Post.Effect
{
    // 自定义组件
    [VolumeComponentMenu("B-Post-processing/Bloom")]
    public class Bloom : B_PostProcessing
    {


        [Tooltip("提取亮度")]
        public FloatParameter Threshold = new FloatParameter(0.6f);
        [Tooltip("控制亮度阈值函数的形状因子")]
        public FloatParameter Knee = new ClampedFloatParameter(0.6f, 0.01f, 1.0f);

        [Tooltip("控制模糊")]
        public FloatParameter blurSpread = new ClampedFloatParameter(0.6f, 0.0f, 3.0f);
        [Tooltip("降采样次数")]
        public IntParameter iterations = new ClampedIntParameter(1, 1, 8);
        

        
        public IntParameter RTDownScaling = new ClampedIntParameter(1, 1, 8);


        // 临时RT
        int BloomtempRT1 = Shader.PropertyToID("_BloomRT1");
        int BloomtempRT2 = Shader.PropertyToID("_BloomRT2");

        private const string mShaderName = "B_Post/Bloom";   


        int[] downSampleRT;                     // 绑定属性控制采样
        int[] upSampleRT;


        // 是否应用后处理
        public override bool IsActive() => mMaterial != null && (IsThresholdActive() || IsBlurSpreadActive());
        // 判断设置颜色
        private bool IsThresholdActive() => Threshold.value != 0.6f;
        private bool IsBlurSpreadActive() => blurSpread.value != 0.6f;


        // 设置渲染流程中的注入点
        public override BasicInjectionPoint InjectionPoint => BasicInjectionPoint.AfterPostProcess;   // 增加到渲染流程的具体阶段
        public override int OrderInInjectionPoint => 20;


        // 配置当前后处理 创建对应的材质
        public override void Setup() 
        {
            if (mMaterial == null)
            
            mMaterial = CoreUtils.CreateEngineMaterial(mShaderName);
        }
        
        
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData) 
        {


            RenderTextureDescriptor inRTDesc = renderingData.cameraData.cameraTargetDescriptor;
            inRTDesc.depthBufferBits = 0;     

            // 定义屏幕尺寸
            var width = (int)(inRTDesc.width);
            var height = (int)(inRTDesc.height);


            // 初始化rt
            cmd.GetTemporaryRT(BloomtempRT1, width, height, 0, FilterMode.Bilinear, RenderTextureFormat.DefaultHDR); 
            cmd.GetTemporaryRT(BloomtempRT2, width, height, 0, FilterMode.Bilinear, RenderTextureFormat.DefaultHDR); 


            // 降采样
            downSampleRT = new int[iterations.value];                     // 绑定属性控制采样
            upSampleRT = new int[iterations.value];

            for (int i = 0; i < iterations.value; i++)
            {
                downSampleRT[i] = Shader.PropertyToID("BloomDownSample" + i);
                upSampleRT[i] = Shader.PropertyToID("BloomUpSample" + i);
            }



            for (int i = 0; i < iterations.value; i++)
            {
                width = Mathf.Max(width / 2, 1);
                height = Mathf.Max(height / 2, 1);
                cmd.GetTemporaryRT(downSampleRT[i], width, height, 0, FilterMode.Bilinear, RenderTextureFormat.Default);
                cmd.GetTemporaryRT(upSampleRT[i], width, height, 0, FilterMode.Bilinear, RenderTextureFormat.Default);
            }

        }



        // 执行渲染逻辑
        public override void Render(CommandBuffer cmd, ref RenderingData renderingData, RTHandle source, RTHandle destination) 
        {
            SetMatData();

            cmd.Blit(source, BloomtempRT1, mMaterial, 0); // 提取亮部信息



            for (int i = 0; i < iterations.value; i++)
            {
                cmd.Blit(BloomtempRT1, downSampleRT[i], mMaterial, 1);                                // 调用第一个 pass 降采样
                BloomtempRT1 = downSampleRT[i];
            }

            // upSample
            for (int j = iterations.value - 1; j >= 0; j--) 
            {
                cmd.Blit(BloomtempRT1, upSampleRT[j], mMaterial, 2); // 使用第二个 pass 升采样
                BloomtempRT1 = upSampleRT[j];
            }


            // 结合原始图像和Bloom效果
            cmd.SetGlobalTexture("_SourceTex", source);
            cmd.Blit(BloomtempRT1, destination, mMaterial, 3); 

            cmd.ReleaseTemporaryRT(BloomtempRT1);
            //Release All tmpRT
            for (int i = 0; i < iterations.value; i++)
            {
                cmd.ReleaseTemporaryRT(downSampleRT[i]);
                cmd.ReleaseTemporaryRT(upSampleRT[i]);
            }

        }

    

        // 清理临时RT
        public override void Dispose(bool disposing) 
        {
            base.Dispose(disposing);
            CoreUtils.Destroy(mMaterial);       
            
        }
        // 绑定属性
        private void SetMatData()
        {

            mMaterial.SetFloat("_Threshold", Threshold.value);               // 获取亮度信息
            mMaterial.SetFloat("_BlurRange", blurSpread.value);               // 获取亮度信息
            mMaterial.SetFloat("_Knee", Knee.value);               // 获取亮度信息

        }

    }

}