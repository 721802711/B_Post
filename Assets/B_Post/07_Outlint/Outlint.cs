using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using B_Post;


namespace B_Post.Effect
{


    public enum OutlintMode
    {
        None,
        Depth, 
        Normal
    }

    // 自定义组件
    [VolumeComponentMenu("B-Post-processing/Outlint")]

    public class Outlint : B_PostProcessing
    {


        [Tooltip("设置模式")]
        public OutlintModeParameter mode = new OutlintModeParameter(OutlintMode.None);

        [Tooltip("边缘颜色")]
        public ColorParameter  OutlintColor = new ColorParameter(Color.white, true);

        [Tooltip("边缘检测大小")]
        public ClampedFloatParameter Scale = new ClampedFloatParameter(1f, 0f, 10f);
        [Tooltip("深度")]
        public ClampedFloatParameter DepthThreshold = new ClampedFloatParameter(0.2f, 1f, 50f);

        [Tooltip("法线深度")]
        public ClampedFloatParameter NormalThreshold = new ClampedFloatParameter(0.4f, 0f, 1.0f);
        public ClampedFloatParameter DepthNormalThreshold = new ClampedFloatParameter(0.5f, 0f, 1f);
        public ClampedFloatParameter DepthNormalThresholdScale = new ClampedFloatParameter(7f, 0f, 10f);

        
        
        private const string mShaderName = "B_Post/Outlint";   

        // 定义关键字   
        private const string mDepthKeyword = "_ADDDEPTH",
                        mNormalKeyword = "_ADDNORMAL";

        // 是否应用后处理
        public override bool IsActive() => mMaterial != null && (IsColorFilterActive());
        // 判断设置颜色
        private bool IsColorFilterActive() => OutlintColor.value != Color.white;



        // 设置渲染流程中的注入点
        public override BasicInjectionPoint InjectionPoint => BasicInjectionPoint.AfterPostProcess;   // 增加到渲染流程的具体阶段
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
            if (renderingData.cameraData.isSceneViewCamera) {
                // 对于场景视图的相机，直接复制源到目标，不应用后处理
                cmd.Blit(source, destination);
                return;
            }

            var camera = renderingData.cameraData.camera;                         // 传入摄像机
            Matrix4x4 clipToView = GL.GetGPUProjectionMatrix(camera.projectionMatrix, true).inverse;
            SetMatData();

            SetKeyword(mDepthKeyword, mode.value == OutlintMode.Depth);
            SetKeyword(mNormalKeyword, mode.value == OutlintMode.Normal);
            cmd.Blit(source,destination, mMaterial, 0);

        }


        // 清理临时RT
        public override void Dispose(bool disposing) 
        {
            base.Dispose(disposing);
            CoreUtils.Destroy(mMaterial);
            
        }

        private void SetMatData()
        {

            mMaterial.SetColor("_Color", OutlintColor.value);
            mMaterial.SetFloat("_Scale", Scale.value);
            mMaterial.SetFloat("_DepthThreshold", DepthThreshold.value);
            mMaterial.SetFloat("_NormalThreshold", NormalThreshold.value);


            mMaterial.SetFloat("_DepthNormalThreshold", DepthNormalThreshold.value);
            mMaterial.SetFloat("_DepthNormalThresholdScale", DepthNormalThresholdScale.value);

        }
    }

    [System.Serializable]
    public sealed class OutlintModeParameter : VolumeParameter<OutlintMode> 
    { 
        public OutlintModeParameter(OutlintMode value, bool overrideState = false) : base(value, overrideState) { } 
    }


}