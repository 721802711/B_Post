using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using B_Post;


namespace B_Post.Effect
{
    // 自定义组件
    [VolumeComponentMenu("B-Post-processing/SSAO")]
    public class SSAO : B_PostProcessing
    {
        public BoolParameter _aoOnly = new BoolParameter(false);
        public ColorParameter aoColor = new ColorParameter(Color.black, false);


        public ClampedIntParameter sampleCount = new ClampedIntParameter(22, 1, 128);
        public ClampedFloatParameter radius = new ClampedFloatParameter(0.5f, 0f, 0.8f);
        public ClampedFloatParameter rangeCheck = new ClampedFloatParameter(0f, 0f, 10f);
        public ClampedFloatParameter aoInt  = new ClampedFloatParameter(1f, 0f, 10f);
        public ClampedFloatParameter blurRadius = new ClampedFloatParameter(1f, 0f, 3f);
        public ClampedFloatParameter bilaterFilterFactor = new ClampedFloatParameter(0.1f, 0f, 1f);



        private const string mShaderName = "B_Post/SSAO";   


        // 是否应用后处理
        public override bool IsActive() => mMaterial != null && (IsAoColorActive() || IsAoOnlyActive() || IsSampleCountActive() || IsRadiusActive() || IsRangeCheckActive() || IsAoIntActive() || IsBlurRadiusActive() || IsBilaterFilterFactorActive());
        // 判断设置颜色
        private bool IsAoColorActive() => aoColor.value != Color.black;
        private bool IsAoOnlyActive() => _aoOnly.value != false;
        private bool IsSampleCountActive() => sampleCount.value != 22;
        private bool IsRadiusActive() => radius.value != 0.5f;
        private bool IsRangeCheckActive() => rangeCheck.value != 0f;
        private bool IsAoIntActive() => aoInt.value != 1f;
        private bool IsBlurRadiusActive() => blurRadius.value != 1f;
        private bool IsBilaterFilterFactorActive() => bilaterFilterFactor.value != 0.1f;



        // 设置渲染流程中的注入点
        public override BasicInjectionPoint InjectionPoint => BasicInjectionPoint.AfterPostProcess;   // 增加到渲染流程的具体阶段
        public override int OrderInInjectionPoint => 30;


        // 配置当前后处理 创建对应的材质
        public override void Setup() 
        {
            if (mMaterial == null)
                mMaterial = CoreUtils.CreateEngineMaterial(mShaderName);
        
        }
        
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData) 
        {

            Camera cam = renderingData.cameraData.camera;

            Matrix4x4 vp_Matrix = cam.projectionMatrix * cam.worldToCameraMatrix;
            mMaterial.SetMatrix("_VPMatrix_invers", vp_Matrix.inverse);

            Matrix4x4 v_Matrix = cam.worldToCameraMatrix;
            mMaterial.SetMatrix("_VMatrix", v_Matrix);

            Matrix4x4 p_Matrix = cam.projectionMatrix;
            mMaterial.SetMatrix("_PMatrix", p_Matrix);


        }
        // 执行渲染逻辑
        public override void Render(CommandBuffer cmd, ref RenderingData renderingData, RTHandle source, RTHandle destination) 
        {

            
            SetMatData();

            // 临时RT
    
            int bufferid1 = Shader.PropertyToID("bufferblur1");         // 临时图像，
            int bufferid2 = Shader.PropertyToID("bufferblur2");         // 临时图像，


            var Width = source.rt.width;
            var Height = source.rt.height;
            
            cmd.GetTemporaryRT(bufferid1, Width, Height, 0, FilterMode.Bilinear, RenderTextureFormat.DefaultHDR); //申请一个临时图像，并设置相机rt的参数进去
            cmd.GetTemporaryRT(bufferid2, Width/ 2, Height/ 2, 0, FilterMode.Bilinear, RenderTextureFormat.DefaultHDR); //申请一个临时图像，并设置相机rt的参数进去

          
            cmd.Blit(source, bufferid1);      

            if (_aoOnly.value)
            {
                cmd.Blit(bufferid1, destination, mMaterial, 0);   
            }
            else
            {

                // AO
                cmd.Blit(source, bufferid1, mMaterial, 0);                                 // 计算AO 储存到 Buffer1

                // 水平模糊
                cmd.SetGlobalTexture("_AOTex", bufferid1);                                           // 计算完的图储存到 bufferid1  
                cmd.Blit(bufferid1, bufferid2, mMaterial, 1);                                   // 进行水平模糊 储存到 Buffer2

                //  垂直模糊
                cmd.SetGlobalTexture("_AOTex", bufferid2);                                           // 把计算完的图传入到Shader中进行下一次计算 
                cmd.Blit(bufferid2, bufferid1, mMaterial, 2);                                   // 进行垂直模糊 储存到 Buffer1            

                // 混合
                cmd.SetGlobalTexture("_AOTex", source);                                           // 计算完结果传入到Shader中 和原图进行处理
                cmd.Blit(bufferid1, destination, mMaterial, 3);                            //  第四个Pass

            }

           cmd.ReleaseTemporaryRT(bufferid1);                                         // 释放临时RT
           cmd.ReleaseTemporaryRT(bufferid2);                                         // 释放临时RT         

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
            mMaterial.SetColor("_aoColor", aoColor.value);               // 获取value 组件的颜色 
            mMaterial.SetFloat("_SampleCount", sampleCount.value);
            mMaterial.SetFloat("_Radius", radius.value);
            mMaterial.SetFloat("_RangeCheck", rangeCheck.value);
            mMaterial.SetFloat("_AOInt", aoInt.value);

            mMaterial.SetFloat("_BlurRadius", blurRadius.value);
            mMaterial.SetFloat("_BilaterFilterFactor", bilaterFilterFactor.value);
        }

    }

}