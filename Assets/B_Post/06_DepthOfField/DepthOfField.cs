using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using B_Post;


namespace B_Post.Effect
{

    public enum DepthMode
    {
        None,
        GaussianDOF, 
        Bokeh
    }

    // 自定义组件
    [VolumeComponentMenu("B-Post-processing/Depth Of Field")]

    public class DepthOfField : B_PostProcessing
    {


        // 白色模糊   黑色清楚
        [Tooltip("设置模式")]
        public DepthModeParameter mode = new DepthModeParameter(DepthMode.None);

        [Tooltip("白色模糊   黑色清楚")]
        public BoolParameter DepthOnly = new BoolParameter(false);


        [Tooltip("整体强度")]
        public ClampedFloatParameter FocusPower = new ClampedFloatParameter(1f, 0f, 1f);
        [Tooltip("控制焦距点")]
        public ClampedFloatParameter DOFDistance = new ClampedFloatParameter(0.65f, 0.5f, 1.0f);
        [Tooltip("焦点大小")]
        public ClampedFloatParameter FarBlurScale = new ClampedFloatParameter(300f, 100f, 500f);
        [Tooltip("颜色对比度")]
        public ClampedFloatParameter FarBlurScalePower = new ClampedFloatParameter(0.5f, 0f, 10f);


        [Range(0f, 10f), Tooltip("模糊的迭代次数")]
        public IntParameter BlurTimes = new ClampedIntParameter(5, 0, 10);
        [Range(0f, 10f), Tooltip("模糊半径")]
        public FloatParameter BlurRange = new ClampedFloatParameter(1.0f, 0.0f, 10.0f);
        [Range(0f, 10f), Tooltip("降采样次数")]
        public IntParameter RTDownSampling = new ClampedIntParameter(1, 1, 10);


        [Space(10)]
        public ClampedFloatParameter End = new ClampedFloatParameter(0.0f, 0.0f, 100.0f);
        public ClampedFloatParameter Start = new ClampedFloatParameter(1.0f, 0.0f, 100.0f);
        public ClampedFloatParameter Density = new ClampedFloatParameter(0.1f, -0.5f, 0.5f);


        [Range(0f, 10f), Tooltip("迭代次数")]
        public IntParameter Iteration = new ClampedIntParameter(1, 1, 10);
        [Range(0f, 10f), Tooltip("像素大小")]
        public IntParameter DownSample = new ClampedIntParameter(1, 1, 10);


        // 创建材质制定Shader路径
        private const string mShaderName = "B_Post/DOF";

        // 定义关键字   
        private const string mDepthOnlyKeyword = "_ADDDEPTH";

        // 是否应用后处理
        public override bool IsActive() 
        {
            return mode.value != DepthMode.None;
        }

        
        // 设置渲染流程中的注入点
        public override BasicInjectionPoint InjectionPoint => BasicInjectionPoint.AfterPostProcess;
        public override int OrderInInjectionPoint => 20;


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



            switch (mode.value)
            {

                case DepthMode.None:
                    break;


                case DepthMode.GaussianDOF:
                    // 根据 DepthOnly 的值来设置关键字
                    SetKeyword(mDepthOnlyKeyword, DepthOnly.value);
                    ApplyBlurEffect(cmd, source,destination,0);   
                    break;


                case DepthMode.Bokeh:
                    cmd.Blit(source,destination, mMaterial, 1);
                    break;

            }

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

            mMaterial.SetFloat("_FocusPower", FocusPower.value);
            mMaterial.SetFloat("_DOFDistance", DOFDistance.value);
            mMaterial.SetFloat("_farBlurScale", FarBlurScale.value);
            mMaterial.SetFloat("_farBlurScalePower", FarBlurScalePower.value);


            mMaterial.SetFloat("_Iteration", Iteration.value);
            mMaterial.SetFloat("_DownSample", DownSample.value);

            mMaterial.SetFloat("_End", End.value);
            mMaterial.SetFloat("_Start", Start.value);
            mMaterial.SetFloat("_Density", Density.value);
        }

        private void ApplyBlurEffect(CommandBuffer cmd, RTHandle sourceRT, RTHandle destinationRT, int passIndex) {
            for (int i = 0; i < BlurTimes.value; i++) {
                // 应用模糊效果，使用指定的Shader Pass
                cmd.Blit(sourceRT, destinationRT, mMaterial, passIndex);
                // 交换渲染目标，以便下一次迭代使用
                RTHandle temp = sourceRT;
                sourceRT = destinationRT;
                destinationRT = temp;
            }
        }

    }
    
    [System.Serializable]
    public sealed class DepthModeParameter : VolumeParameter<DepthMode> 
    { 
        public DepthModeParameter(DepthMode value, bool overrideState = false) : base(value, overrideState) { } 
    }

}