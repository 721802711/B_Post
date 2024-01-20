using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using B_Post;



namespace B_Post.Effect
{
    // 自定义组件
    [VolumeComponentMenu("B-Post-processing/Color Adiustment")]

    public class ColorAdiustment : B_PostProcessing
    {

        private const string mShaderName = "B_Post/ColorAdjustment";


        public ClampedFloatParameter brightness = new ClampedFloatParameter(1.0f, 0, 3);
        public ClampedFloatParameter saturation = new ClampedFloatParameter(1.0f, 0, 3);
        public ClampedFloatParameter contrast = new ClampedFloatParameter(1.0f, 0, 3);


        // 是否应用后处理
        public override bool IsActive() => mMaterial != null && (IsBrightnessActive() || IsSaturationActive() || IsContrastActive());
        // 判断设置颜色
        private bool IsBrightnessActive() => brightness.value != 1.0f;
        private bool IsSaturationActive() => saturation.value != 1.0f;
        private bool IsContrastActive() => contrast.value != 1.0f;

        // 设置渲染流程中的注入点
        public override BasicInjectionPoint InjectionPoint => BasicInjectionPoint.BeforePostProcess;
        public override int OrderInInjectionPoint => 10;


        // 配置当前后处理 创建对应的材质
        public override void Setup() 
        {
            if (mMaterial == null)
            
            mMaterial = CoreUtils.CreateEngineMaterial(mShaderName);
        }
        
        // 执行渲染逻辑
        public override void Render(CommandBuffer cmd, ref RenderingData renderingData, RTHandle source, RTHandle destination) 
        {
            if (mMaterial == null) return;

                mMaterial.SetFloat("_Brightness", brightness.value);
                mMaterial.SetFloat("_Saturation", saturation.value);
                mMaterial.SetFloat("_Contrast", contrast.value);
                cmd.Blit(source,destination, mMaterial, 0);
        }



        // 清理临时RT
        public override void Dispose(bool disposing) 
        {
            base.Dispose(disposing);
            CoreUtils.Destroy(mMaterial);
            

        }

    }

}