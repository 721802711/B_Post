Shader "B_Post/Bloom"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Threshold ("Thresold", Range(0, 1)) = 0.8
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"}
        LOD 100

        Cull Off ZWrite Off ZTest Always

        // 第一个Pass
        pass
        {
            name "Bloom PreFilterPass"                // 提取亮度
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment PreFilterfrag

            #include "BloomCommon.hlsl"           //函数库

            ENDHLSL        
		}
        
        // 第二个Pass
        pass
        {
            name "Bloom PrefilterFirePass"              // 过滤Fireflies（光斑)
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment PrefilterFirefrag

            #include "BloomCommon.hlsl"           //函数库

            ENDHLSL        
		}

        // 第三个Pass
        pass
        {
            name "Bloom BoxBlurPass"                // 视频模糊

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment BoxBlurfrag

            #include "BloomCommon.hlsl"           //函数库

            ENDHLSL        
		}

    
        // 第4个Pass
        pass
        {
            
            name "Bloom MergePass"                // 合并处理

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment Mergefrag

            #pragma shader_feature _BLOOMADDTIVE
            
            #include "BloomCommon.hlsl"           //函数库

            ENDHLSL        
		}
        
    }    
}