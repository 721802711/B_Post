#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"



CBUFFER_START(UnityPerMaterial)
float4 _MainTex_ST;
float2 _MainTex_TexelSize;
float _BlurRange;
float blurrange;
CBUFFER_END


TEXTURE2D(_MainTex);             SAMPLER(sampler_MainTex);


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

// 高斯模糊
half4 Gaussianfrag(v2f i) : SV_Target
{

    float4 col = float4(0, 0, 0, 0);
    blurrange = _BlurRange / 300;
    
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(0.0, 0.0)) * 0.147716f;
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(blurrange, 0.0)) * 0.118318f;
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(0.0, -blurrange)) * 0.118318f;
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(0.0, blurrange)) * 0.118318f;
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(-blurrange, 0.0)) * 0.118318f;

    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(blurrange, blurrange)) * 0.0947416f;
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(-blurrange, -blurrange)) * 0.0947416f;
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(blurrange, -blurrange)) * 0.0947416f;
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(-blurrange, blurrange)) * 0.0947416f;

    return col;
}


//  方框模糊 的片元着色器阶段
half4 Boxfrag(v2f i) : SV_Target
{

    float4 col = float4(0, 0, 0, 0);
    float2 UV_Offset;

    float Box_Weight = 0.11111;

    for (int x = -1; x <= 1; x++)
    {
        for (int y = -1; y <= 1; y++)
        {
            UV_Offset = i.uv;
            UV_Offset.x += x * _MainTex_TexelSize.x * _BlurRange / 3;
            UV_Offset.y += y * _MainTex_TexelSize.y * _BlurRange / 3;
            col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, UV_Offset);
        }
    }
    col *= 0.11111;
    return col;
}


// Kawasefrag

half4 Kawasefrag(v2f i) : SV_Target
{
float4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex,i.uv);
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(-1, -1) * _MainTex_TexelSize.xy * _BlurRange);
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(1, -1) * _MainTex_TexelSize.xy * _BlurRange);
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(-1, 1) * _MainTex_TexelSize.xy * _BlurRange);
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(1, 1) * _MainTex_TexelSize.xy * _BlurRange);
    col /= 5;
    return col;
}