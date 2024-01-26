Shader "B_Post/SSAO"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _aoColor("aoColor", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"}
        LOD 100

        Cull Off ZWrite Off ZTest Always


        // 第一个 pass 用于计算 AO
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag_ao

            #include "AOPass.hlsl"

            ENDHLSL
        }

        // 第二个 pass  水平方向
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert_h
            #pragma fragment frag_Blur

            #include "BlurPass.hlsl"

            ENDHLSL
        }
        // 第三个 pass 垂直方向
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert_v
            #pragma fragment frag_Blur

            #include "BlurPass.hlsl"

            ENDHLSL
        }


        // 第四个 pass用于将AO和原图混合
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag_final


            #include "AOPass.hlsl"


            ENDHLSL
        }

    }
}