Shader "B_Post/Blur"
{
    Properties
    {
        _MainTex ("_MainTex", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
         Cull Off ZWrite Off ZTest Always


        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment Gaussianfrag

            #include "Blur.hlsl"           //函数库


            ENDHLSL
        }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment Boxfrag

            #include "Blur.hlsl"           //函数库


            ENDHLSL
        }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment Kawasefrag

            #include "Blur.hlsl"           //函数库


            ENDHLSL
        }
    }
}