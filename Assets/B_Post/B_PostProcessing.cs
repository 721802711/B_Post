using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


namespace B_Post
{

    // 声明注入点 
    public enum BasicInjectionPoint
    {
        AfterOpauqe,
        AfterSkybox,
        BeforePostProcess,
        AfterPostProcess
    }

    // 抽象基类 接口
    public abstract class B_PostProcessing : VolumeComponent, IPostProcessComponent, IDisposable
    {

        // 注入点
        public virtual BasicInjectionPoint InjectionPoint => BasicInjectionPoint.AfterPostProcess;

        // 设置注入点的顺序
        public virtual int OrderInInjectionPoint => 0;


        // 配置当前后处理
        public abstract void Setup();

        // 当相机初始化时执行
        public virtual void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData) {
        }
        
        // 执行渲染
        public abstract void Render(CommandBuffer cmd, ref RenderingData renderingData, RTHandle source, RTHandle destination);


        #region IPostProcessComponent
        // 表明它是一个后处理组件，可以插入到URP或HDRP的后处理流程中
        // 后处理是否激活
        public abstract bool IsActive();
        
        public virtual bool IsTileCompatible() => false;
        #endregion

        #region IDisposable  
        // IDisposable接口用于释放资源，防止资源泄漏。
        public void Dispose() {  
            Dispose(true);  
            GC.SuppressFinalize(this);  
        }  
        
        public virtual void Dispose(bool disposing) 
        {  
        }  
        #endregion


    }

}