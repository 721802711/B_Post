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
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment PreFilterfrag

            #include "BloomCommon.hlsl"           //函数库

            ENDHLSL        
		}
        
        // 第二个Pass
        pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment BoxBlurfrag

            #include "BloomCommon.hlsl"           //函数库

            ENDHLSL        
		}
        // 第三个Pass
        pass
        {

            blend one one                // 主要是这里

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment AddBlurfrag

            #include "BloomCommon.hlsl"           //函数库

            ENDHLSL        
		}

        // 第四个Pass
        pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment Mergefrag

            #include "BloomCommon.hlsl"           //函数库

            ENDHLSL        
		}
        
    }    
}