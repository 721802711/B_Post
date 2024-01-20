#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"



CBUFFER_START(UnityPerMaterial)
float _Blur;
float _Loop;
float _Y;
float _X;
float _Instensity;
float _BufferRadius;
CBUFFER_END


TEXTURE2D(_MainTex);             SAMPLER(sampler_MainTex);
TEXTURE2D(_SourceTex);             SAMPLER(sampler_SourceTex);

struct appdata
{
    float4 positionOS : POSITION;
    float2 texcoord : TEXCOORD0;
};

struct v2f
{
    float2 uv : TEXCOORD0;
    float4 vertex : SV_POSITION;

};


v2f vert(appdata v)
{
    v2f o;
    o.vertex = TransformObjectToHClip(v.positionOS.xyz);
    o.uv = v.texcoord;
    return o;
}

half4 radialfrag(v2f i) : SV_Target
{

    float4 col = 0;
    float2 dir = (float2(_X,_Y) - i.uv) * _Blur * 0.01;
    float blurParams = saturate(distance(i.uv,float2(_X,_Y)) / _BufferRadius);   // 控制不模糊的半径

    for(int t = 0; t < _Loop; t++) 
    {
        col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + dir * t * blurParams)/ _Loop;
    }
    return col;
}
