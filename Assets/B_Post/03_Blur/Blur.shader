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
        Pass
        {
            Name "DownSample"
            HLSLPROGRAM
            #pragma vertex DualKawaseDownvert
            #pragma fragment DualKawaseDownfrag

            #include "Blur.hlsl"           //函数库


            ENDHLSL
        }

        Pass
        {
            Name "UpSample"
            HLSLPROGRAM
            #pragma vertex DualKawaseUpvert
            #pragma fragment DualKawaseUpfrag

            #include "Blur.hlsl"           //函数库


            ENDHLSL
        }

    }
}