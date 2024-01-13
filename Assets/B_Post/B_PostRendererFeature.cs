using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;


namespace B_Post
{
    public class B_PostRendererFeature : ScriptableRendererFeature
    {
        // 获取后处理基类列表
        private List<B_PostProcessing> mB_PostProcessings;

        // 注入Pass
        private B_PostProcessPass m_AfterOpaquePass;
        private B_PostProcessPass m_AfterSkyboxPass;
        private B_PostProcessPass m_BeforePostProcessPass;
        private B_PostProcessPass m_AfterPostProcessPass;



        public override void Create() 
        {

            var stack = VolumeManager.instance.stack;
            // 获取 所有继承自 BasicPostProcessing 类型的Volume组件 并增加到列表中
            mB_PostProcessings = VolumeManager.instance.baseComponentTypeArray
            .Where(t => t.IsSubclassOf(typeof(B_PostProcessing)))
            .Select(t => stack.GetComponent(t) as B_PostProcessing)
            .ToList();


            // 设置 AfterOpaque 的后处理效果
            var afterOpaqueCPPs = mB_PostProcessings
                .Where(c => c.InjectionPoint == BasicInjectionPoint.AfterOpauqe)
                .OrderBy(c => c.OrderInInjectionPoint)     // 筛选出 效果进行排序
                .ToList(); 

                // afterOpaqueCPPs 储存所有 AfterOpauqe 类型排序后的新列表
            m_AfterOpaquePass = new B_PostProcessPass("不透明物体之后", afterOpaqueCPPs);
            // 对应时机
            m_AfterOpaquePass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;


            // 筛选出设置为 AfterSkybox 的后处理效果
            var afterSkyboxCPPs = mB_PostProcessings
                .Where(c => c.InjectionPoint == BasicInjectionPoint.AfterSkybox)
                .OrderBy(c => c.OrderInInjectionPoint)     // 筛选出 效果进行排序
                .ToList();
            
            m_AfterSkyboxPass = new B_PostProcessPass(" 天空盒之后 ",  afterSkyboxCPPs);
            // 对应时机
            m_AfterSkyboxPass.renderPassEvent = RenderPassEvent.AfterRenderingSkybox;


            // 筛选出设置为 BeforePostProcess 的后处理效果
            var beforePostProcessingCPPs = mB_PostProcessings
                .Where(c => c.InjectionPoint == BasicInjectionPoint.BeforePostProcess)
                .OrderBy(c => c.OrderInInjectionPoint)    
                .ToList();

            m_BeforePostProcessPass = new B_PostProcessPass(" 后处理之后 ",  beforePostProcessingCPPs);
            // 对应时机
            m_BeforePostProcessPass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

            
            // 筛选出设置为 AfterPostProcess 的后处理效果
            var afterPostProcessCPPs = mB_PostProcessings
                .Where(c => c.InjectionPoint == BasicInjectionPoint.AfterPostProcess)
                .OrderBy(c => c.OrderInInjectionPoint)    
                .ToList();

            m_AfterPostProcessPass = new B_PostProcessPass(" 渲染最后阶段 ",  afterPostProcessCPPs);
            // 对应时机
            m_AfterPostProcessPass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;

        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {

            // 当前摄像机是否开启后处理
            if (renderingData.cameraData.postProcessEnabled)
            {
                // 并且将pass列表加到renderer中
                if (m_AfterOpaquePass.SetupPostProcessing()) {
                    m_AfterOpaquePass.ConfigureInput(ScriptableRenderPassInput.Color);
                    renderer.EnqueuePass(m_AfterOpaquePass);
                }

                if (m_AfterSkyboxPass.SetupPostProcessing()) {
                    m_AfterSkyboxPass.ConfigureInput(ScriptableRenderPassInput.Color);
                    renderer.EnqueuePass(m_AfterSkyboxPass);
                }

                if (m_BeforePostProcessPass.SetupPostProcessing()) {
                    m_BeforePostProcessPass.ConfigureInput(ScriptableRenderPassInput.Color);
                    renderer.EnqueuePass(m_BeforePostProcessPass);
                }

                if (m_AfterPostProcessPass.SetupPostProcessing()) {
                    m_AfterPostProcessPass.ConfigureInput(ScriptableRenderPassInput.Color);
                    renderer.EnqueuePass(m_AfterPostProcessPass);
                }
            }

        }

        //  释放资源
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);


            m_AfterSkyboxPass.Dispose();
            m_BeforePostProcessPass.Dispose();
            m_AfterPostProcessPass.Dispose();

            if (mB_PostProcessings != null)
            {
                foreach(var item in mB_PostProcessings)
                {
                    item.Dispose();
                }
            }
        }


    }

}