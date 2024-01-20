Shader "B_Post/ACES"
{
    Properties
    {
        _MainTex ("_MainTex", 2D) = "white" {}
        _FilmSlope("_FilmSlope", float) = 2.51
        _FilmToe("_FilmToe", float) = 0.03
        _FilmShoulder("_FilmShoulder", float) = 2.43
        _FilmBlackClip("_FilmBlackClip", float) = 0.59
        _FilmWhiteClip("_FilmWhiteClip", float) = 0.14
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "ACES.hlsl"           //函数库


            ENDHLSL
        }
    }
}

