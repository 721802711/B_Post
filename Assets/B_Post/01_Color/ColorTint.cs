using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using B_Post;


namespace B_Post.Effect
{
    // 自定义组件
    [VolumeComponentMenu("B-Post-processing/Color Blit")]

    public class ColorTint : B_PostProcessing
    {


        private const string mShaderName = "B_Post/Color";   

        // 设置颜色参数
        public ColorParameter ColorChange = new ColorParameter(Color.white, true);  

        // 是否应用后处理
        public override bool IsActive() => mMaterial != null && (IsColorFilterActive());
        // 判断设置颜色
        private bool IsColorFilterActive() => ColorChange.value != Color.white;



        // 设置渲染流程中的注入点
        public override BasicInjectionPoint InjectionPoint => BasicInjectionPoint.AfterPostProcess;   // 增加到渲染流程的具体阶段
        public override int OrderInInjectionPoint => 15;


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


            if (mMaterial == null) return;
                mMaterial.SetColor("_ColorTint", ColorChange.value);
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