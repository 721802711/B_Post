Shader "B_Post/RadialBlur"
{
    Properties
    {
        _MainTex ("_MainTex", 2D) = "white" {}
        _Blur("_Blur",Float) = 0
        [int]_Loop("_Loop",range(1,10)) = 1
        _X("_X",Float) = 0.5
        _Y("_Y",Float) = 0.5
        _Instensity("_Instensity",Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }


        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment radialfrag

            #include "RadialBlur.hlsl"           //º¯Êý¿â


            ENDHLSL
        } 
        
    }
}