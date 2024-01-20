using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using B_Post;


namespace B_Post.Effect
{

    public class PostFramework : B_PostProcessing
    {


        private const string mShaderName = "";   



        // 是否应用后处理
        public override bool IsActive() => mMaterial;
        // 判断设置颜色
        private bool IsColorFilterActive() => true;



        // 设置渲染流程中的注入点
        public override BasicInjectionPoint InjectionPoint => BasicInjectionPoint.AfterPostProcess;   // 增加到渲染流程的具体阶段
        public override int OrderInInjectionPoint => 0;


        // 配置当前后处理 创建对应的材质
        public override void Setup() 
        {

        }
        
        // 执行渲染逻辑
        public override void Render(CommandBuffer cmd, ref RenderingData renderingData, RTHandle source, RTHandle destination) 
        {

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

        }

    }

}