using B_Post;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace B_Post.Effect
{

    public enum BlurMode
    {
        None,
        GaussianBlur,  
        BilateralFilter
    }

    // 自定义组件
    [System.Serializable, VolumeComponentMenuForRenderPipeline("B-Post-processing/Volumetric Light", typeof(UniversalRenderPipeline))] 
    public class VolumetricLight : B_PostProcessing 
    {

        // 定义属性
        public ColorParameter ColorChange = new ColorParameter(Color.white, true);
        [Range(0, 3)]
        public ClampedFloatParameter lightIntensity = new ClampedFloatParameter(0.05f, 0f, 0.1f);       // 体积光确定
        public ClampedFloatParameter stepSize = new ClampedFloatParameter(0.1f, 0.1f, 0.5f);                                      // 步长大小
        public FloatParameter maxDistance = new FloatParameter(1000);                                   // 最大渲染距离
        public IntParameter maxStep = new IntParameter(500);                                            // 最大迭代次数


        
        [Tooltip("设置模糊方式")]
        public EnumModeParameter mode = new EnumModeParameter(BlurMode.None);

        // 这里是高斯模糊的属性
        public ClampedIntParameter loop = new ClampedIntParameter(3, 1, 10);
        public ClampedFloatParameter BlurInt = new ClampedFloatParameter(0.3f, 0.0f, 1f);


        // 这里是 双边滤波的属性
        public ClampedFloatParameter Space_S = new ClampedFloatParameter(0.3f, 0.1f, 5f);
        public ClampedFloatParameter Space_R = new ClampedFloatParameter(0.3f, 0.1f, 5f);
        public ClampedFloatParameter KernelSize = new ClampedFloatParameter(0.5f, 0.1f, 50f);


        // 创建材质制定Shader路径
        private const string mShaderName = "B_Post/VolumetricShader";   

        // 设置渲染流程中的注入点
        public override BasicInjectionPoint InjectionPoint => BasicInjectionPoint.AfterPostProcess;
        public override int OrderInInjectionPoint => 6;

        // 是否应用后处理
        public override bool IsActive() => mMaterial != null  && (IsColorFilterActive() || IsLightIntensityActive() || IsStepSizeActive() || IsMaxDistanceActive() || IsMaxStepActive());
        // 判断设置颜色
        private bool IsColorFilterActive() => ColorChange.value != Color.white;        

        private bool IsLightIntensityActive() => lightIntensity.value != 0.05f;
        private bool IsStepSizeActive() => stepSize.value != 0.1f;
        private bool IsMaxDistanceActive() => maxDistance.value != 1000;
        private bool IsMaxStepActive() => maxStep.value != 500;


        // 配置当前后处理
        public override void Setup() 
        {
            if (mMaterial == null)
            
            mMaterial = CoreUtils.CreateEngineMaterial(mShaderName);
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData) 
        {


        }


        // 执行渲染逻辑
        public override void Render(CommandBuffer cmd, ref RenderingData renderingData, RTHandle source, RTHandle destination) 
        {

            if (renderingData.cameraData.isSceneViewCamera) {
                // 对于场景视图的相机，直接复制源到目标，不应用后处理
                cmd.Blit(source, destination);
                return;
            }


            if (mMaterial == null) return;
            SetMatData();
    
            // 获取临时渲染纹理标识符
            int temporaryRT1 = Shader.PropertyToID("Temp1");
            int temporaryRT2 = Shader.PropertyToID("Temp2");


            // 设置临时纹理描述符
            RenderTextureDescriptor rtDesc = renderingData.cameraData.cameraTargetDescriptor;
            rtDesc.depthBufferBits = 0;

            // 分配临时纹理
            cmd.GetTemporaryRT(temporaryRT1, rtDesc);
            cmd.GetTemporaryRT(temporaryRT2, rtDesc);



            // 复制原始图像到临时纹理
            cmd.Blit(source, temporaryRT1, mMaterial, 0);

            switch (mode.value)
            {
                case BlurMode.None:
                    // 无模糊
                    break;
                case BlurMode.GaussianBlur:
                    // 高斯模糊
                    for (int i = 0; i < loop.value; i++)
                    {
                        cmd.Blit(temporaryRT1, temporaryRT2, mMaterial, 1);
                        cmd.Blit(temporaryRT2, temporaryRT1);
                    }
                    break;
                case BlurMode.BilateralFilter:
                    // 双边滤波
                    for (int i = 0; i < loop.value; i++)
                    {
                        cmd.Blit(temporaryRT1, temporaryRT2, mMaterial, 2);
                        cmd.Blit(temporaryRT2, temporaryRT1);
                    }
                    break;
            }



            cmd.SetGlobalTexture("_FinalTex", source);  
            // 最终将处理过的图像复制回原始目标
            cmd.Blit(temporaryRT1, destination, mMaterial, 3);


            // 释放临时纹理
            cmd.ReleaseTemporaryRT(temporaryRT1);
            cmd.ReleaseTemporaryRT(temporaryRT2);

        }
        // 清理临时RT
        public override void Dispose(bool disposing) 
        {
            base.Dispose(disposing);
            CoreUtils.Destroy(mMaterial);       
        }


        private void SetMatData()
        {
            mMaterial.SetInt("_MaxStep", maxStep.value);               
            mMaterial.SetFloat("_MaxDistance", maxDistance.value);
            mMaterial.SetFloat("_LightIntensity", lightIntensity.value);
            mMaterial.SetFloat("_StepSize", stepSize.value);
            mMaterial.SetColor("_Color", ColorChange.value);
            mMaterial.SetFloat("_BlurInt", BlurInt.value);



            mMaterial.SetFloat("_Space_Sigma", Space_S.value);
            mMaterial.SetFloat("_Range_Sigma", Space_R.value);
            mMaterial.SetFloat("_KernelSize", KernelSize.value);

        }


    }
    [System.Serializable]
    public sealed class EnumModeParameter : VolumeParameter<BlurMode> 
    { 
        public EnumModeParameter(BlurMode value, bool overrideState = false) : base(value, overrideState) { } 
    }

}