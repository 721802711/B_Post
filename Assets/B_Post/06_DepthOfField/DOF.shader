Shader "B_Post/DOF"
{
    Properties
    {

        _MainTex ("Base (RGB)", 2D) = "white" { }

    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment DOFfrag

            #pragma shader_feature _ADDDEPTH


            #include "DepthOfField.hlsl"           //º¯Êý¿â


            ENDHLSL
        }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment Bokehfrag

            #include "DepthOfField.hlsl"           //º¯Êý¿â


            ENDHLSL
        }

   }

}