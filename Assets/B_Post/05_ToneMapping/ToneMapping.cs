using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using B_Post;


namespace B_Post.Effect
{

    public enum ToneMode
    {
        None,
        ACES,  
    }


    // 自定义组件
    [VolumeComponentMenu("B-Post-processing/Tone Mapping")]

    public class ToneMapping : B_PostProcessing
    {
        [Tooltip("设置模式")]
        public ToneModeParameter mode = new ToneModeParameter(ToneMode.None);

        public FloatParameter PostExposure = new FloatParameter(0.6f);

        [Tooltip("Film_Slope")]
        public ClampedFloatParameter slope = new ClampedFloatParameter(2.51f, 0f, 3f);

        [Tooltip("Film_Toe")]
        public ClampedFloatParameter toe = new ClampedFloatParameter(0.03f, 0.0f, 1.0f);

        [Tooltip("Film_Shoulder")]
        public ClampedFloatParameter shoulder = new ClampedFloatParameter(2.43f, 0.0f, 3.0f);

        [Tooltip("Film_BlackClip")]
        public ClampedFloatParameter blackClip = new ClampedFloatParameter(0.59f, 0.0f, 1.0f);

        [Tooltip("Film_WhiteClip")]
        public ClampedFloatParameter whiteClip = new ClampedFloatParameter(0.14f, 0.0f, 1.0f);



        private const string mShaderName = "B_Post/ACES";   


        // 是否应用后处理
        public override bool IsActive() 
        {
            return mode.value != ToneMode.None;
        }
        // 判断是否开启
        private bool IsPostExposure() => PostExposure.value != 0.6f;    
        private bool IsSlope() => slope.value != 2.51f;    
        private bool IsToe() => toe.value != 0.03f;    
        private bool IsShoulder() => shoulder.value != 2.43f;    
        private bool IsBlackClip() => blackClip.value != 0.59f;    
        private bool IsWhiteClip() => whiteClip.value != 0.14f;  


        // 设置渲染流程中的注入点
        public override BasicInjectionPoint InjectionPoint => BasicInjectionPoint.AfterPostProcess;
        public override int OrderInInjectionPoint => 18;


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



                cmd.Blit(source,destination,mMaterial,0);
        }


        // 清理临时RT
        public override void Dispose(bool disposing) 
        {
            base.Dispose(disposing);
            CoreUtils.Destroy(mMaterial);
            
        }
        private void SetMatData()
        {
            // 属性绑定
            mMaterial.SetFloat("_postExposure", PostExposure.value);                       // Shader变量  和 Volume 组件属性 绑定

            mMaterial.SetFloat("_FilmSlope", slope.value);                       // Shader变量  和 Volume 组件属性 绑定
            mMaterial.SetFloat("_FilmToe", toe.value);                           // Shader变量  和 Volume 组件属性 绑定
            mMaterial.SetFloat("_FilmShoulder", shoulder.value);                 // Shader变量  和 Volume 组件属性 绑定
            mMaterial.SetFloat("_FilmBlackClip", blackClip.value);               // Shader变量  和 Volume 组件属性 绑定
            mMaterial.SetFloat("_FilmWhiteClip", whiteClip.value);   

        }

    }
    
    [System.Serializable]
    public sealed class ToneModeParameter : VolumeParameter<ToneMode> 
    { 
        public ToneModeParameter(ToneMode value, bool overrideState = false) : base(value, overrideState) { } 
    }

}