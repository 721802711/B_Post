using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using B_Post;


namespace B_Post.Effect
{
    // 自定义组件
    [VolumeComponentMenu("B-Post-processing/Radial Blur")]

    public class RadialBlur : B_PostProcessing
    {

        private const string mShaderName = "B_Post/RadialBlur";   


        [Tooltip("模糊中心点")]
        public FloatParameter X = new FloatParameter(0.5f);
        public FloatParameter Y = new FloatParameter(0.5f);

        [Range(0f, 10f), Tooltip("模糊的迭代次数")]
        public IntParameter      BlurTimes = new ClampedIntParameter(1, 1, 10);  
        [Range(0f, 10f), Tooltip("模糊效果")]
        public FloatParameter    BlurRange = new ClampedFloatParameter(0.0f, 0.0f, 10.0f);  
        [Range(0f, 10f), Tooltip("中心模糊半径")]
        public FloatParameter BufferRadius = new ClampedFloatParameter(1.0f, 0.0f, 5.0f);

        // 是否应用后处理
        public override bool IsActive() => mMaterial != null && (IsBlurTimesActive() || IsBlurRangeActive() || IsBufferRadius());

        // 判断是否开启
        private bool IsBlurTimesActive() => BlurTimes.value != 1;    
        private bool IsBlurRangeActive() => BlurRange.value != 0.0;    
        private bool IsBufferRadius() => BufferRadius.value != 1;    


        // 设置渲染流程中的注入点
        public override BasicInjectionPoint InjectionPoint => BasicInjectionPoint.AfterPostProcess;
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
                SetMatData();
                cmd.Blit(source,destination, mMaterial,0);
        }


        // 清理临时RT
        public override void Dispose(bool disposing) 
        {
            base.Dispose(disposing);
            CoreUtils.Destroy(mMaterial);
            
        }
        private void SetMatData()
        {

            mMaterial.SetFloat("_Loop", BlurTimes.value);             // Shader变量  和 Volume 组件属性 绑定
            mMaterial.SetFloat("_X", X.value);                         // Shader变量  和 Volume 组件属性 绑定
            mMaterial.SetFloat("_Y", Y.value);                         // Shader变量  和 Volume 组件属性 绑定
            mMaterial.SetFloat("_Blur", BlurRange.value);             // Shader变量  和 Volume 组件属性 绑定
            mMaterial.SetFloat("_BufferRadius", BufferRadius.value);     // Shader变量  和 Volume 组件属性 绑定
        }

    }

}