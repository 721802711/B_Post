using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using B_Post;
using System;

namespace B_Post.Effect
{


    public enum BloomMode
    {
        Addtive,
        Scatter
    }

    // 自定义组件
    [VolumeComponentMenu("B-Post-processing/Bloom")]
    public class Bloom : B_PostProcessing
    {
        // pass枚举
        private enum PassEnum
        {
            BloomPreFilterPass,           // 0      
            BloomPrefilterFirePass,       // 1   
            BloomBoxBlurPass,             // 2
            BloomMergePass                // 3  
        }

        private const int MAXITERATION = 8;

        [Tooltip("迭代次数")]
        public ClampedIntParameter Iteration = new ClampedIntParameter(0, 0, MAXITERATION);
        [Tooltip("控制强度")]
        public ClampedFloatParameter Intensity = new ClampedFloatParameter(0.0f, 0.0f, 6.0f);
        [Tooltip("颜色")]
        public ColorParameter BloomColor = new ColorParameter(Color.white);

        [Tooltip("提取亮度")]
        public FloatParameter Threshold = new FloatParameter(0.6f);
        [Tooltip("阈值附近亮度值的平滑处理")]
        public ClampedFloatParameter ThresholdKnee = new ClampedFloatParameter(0.1f, 0.1f, 1.0f);
        [Tooltip("用于决定是否启用淡化光斑功能")]
        public BoolParameter FadeFireFlies = new BoolParameter(false);

        [Tooltip("叠加模式")]
        public BloomModeParameter Mode = new BloomModeParameter(BloomMode.Addtive, false);

        [Tooltip("控制模糊")]
        public FloatParameter blurSpread = new ClampedFloatParameter(0.6f, 0.0f, 3.0f);



        [Tooltip("RT 降采样比例")]
        public IntParameter RTDownScaling = new ClampedIntParameter(1, 1, 8);


        // 临时RT
        int BloomtempRT1 = Shader.PropertyToID("_BloomRT1");

        private const string mShaderName = "B_Post/Bloom";   


        // 定义关键字   
        private const string mBloomAddtiveKeyword = "_BLOOMADDTIVE";


        int[] downSampleRT;                     // 绑定属性控制采样
        int[] upSampleRT;


        // 是否应用后处理
        public override bool IsActive() => mMaterial != null && (IsThresholdActive());
        // 判定是否需要模糊
        private bool IsThresholdActive() => Threshold.value != 0.6f;



        // 设置渲染流程中的注入点
        public override BasicInjectionPoint InjectionPoint => BasicInjectionPoint.BeforePostProcess;   // 增加到渲染流程的具体阶段
        public override int OrderInInjectionPoint => 1;


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
            var width = (int)(inRTDesc.width) / RTDownScaling.value;
            var height = (int)(inRTDesc.height) / RTDownScaling.value;


            // 初始化rt
            cmd.GetTemporaryRT(BloomtempRT1, width, height, 0, FilterMode.Bilinear, RenderTextureFormat.DefaultHDR); 
            // 降采样
            downSampleRT = new int[Iteration.value];                     // 绑定属性控制采样
            upSampleRT = new int[Iteration.value];

            for (int i = 0; i < Iteration.value; i++)
            {
                downSampleRT[i] = Shader.PropertyToID("BloomDownSample" + i);
                upSampleRT[i] = Shader.PropertyToID("BloomUpSample" + i);
            }



            for (int i = 0; i < Iteration.value; i++)
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

            cmd.Blit(source, BloomtempRT1, mMaterial, FadeFireFlies.value ? (int)PassEnum.BloomPrefilterFirePass:(int)PassEnum.BloomPreFilterPass); // 提取亮部信息


            //downSample
            for (int i = 0; i < Iteration.value; i++)
            {

                cmd.Blit(BloomtempRT1, downSampleRT[i], mMaterial, (int)PassEnum.BloomBoxBlurPass);                                // 调用第一个 pass 降采样
                BloomtempRT1 = downSampleRT[i];
            }

            //upSample
            for (int j = Iteration.value - 2; j >= 0; j--)            // 注意，这里是j 输入的是的降采样 
            {
                cmd.Blit(BloomtempRT1, upSampleRT[j], mMaterial, (int)PassEnum.BloomBoxBlurPass);                                  // 调用第二个 pass 降采样
                BloomtempRT1 = upSampleRT[j];
            }



            // UpSample
            SetKeyword(mBloomAddtiveKeyword, Mode.value == BloomMode.Addtive);

            cmd.SetGlobalTexture("_SourceTex", source);                                           // 渲染原图 储存到 destination
            cmd.Blit(BloomtempRT1, destination, mMaterial, (int)PassEnum.BloomMergePass); // 合并图像

            cmd.ReleaseTemporaryRT(BloomtempRT1);

            // 释放RT
            for (int i = 0; i < Iteration.value; i++)
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

            mMaterial.SetFloat("_Threshold", Threshold.value);                         // 获取亮度信息
            mMaterial.SetFloat("_BlurRange", blurSpread.value);                        // 模糊
            mMaterial.SetFloat("_Knee", ThresholdKnee.value);                          // 控制亮度平滑
            mMaterial.SetFloat("_Intensity", Intensity.value);                         // Bloom强度
            mMaterial.SetColor("_BloomColor", BloomColor.value);                       // Bloom颜色
        }

    }


    [Serializable]
    public sealed class BloomModeParameter : VolumeParameter<BloomMode>
    {
        public BloomModeParameter(BloomMode value, bool overrideState = false) : base(value, overrideState) 
        {
        }
    }


}