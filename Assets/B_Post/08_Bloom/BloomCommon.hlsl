#ifndef BLOOM_COMMON_INCLUDED
#define BLOOM_COMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"


CBUFFER_START(UnityPerMaterial)
float4 _MainTex_ST;
float _Threshold;
float4 _MainTex_TexelSize;
float _BlurRange;
float _Knee;
CBUFFER_END

struct appdata
{
    float4 positionOS : POSITION;
    float2 texcoord : TEXCOORD0;
};

struct v2f
{
    float2 uv : TEXCOORD0;
    float4 positionCS : SV_POSITION;
};

TEXTURE2D(_MainTex);                          SAMPLER(sampler_MainTex);
TEXTURE2D(_SourceTex);                          SAMPLER(sampler_SourceTex);



half3 BoxFliter(float2 uv, float t)
{
    half2 upL, upR, downL, downR;
   // 计算偏移量
    upL = _MainTex_TexelSize.xy * half2(t, 0);
    upR = _MainTex_TexelSize.xy * half2(0, t);
    downL = _MainTex_TexelSize.xy * half2(-t, 0);
    downR = _MainTex_TexelSize.xy * half2(0, -t);

    half3 col = 0;
   // 平均盒体采样
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + upL).rgb * 0.25;
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + upR).rgb * 0.25;
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + downL).rgb * 0.25;
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + downR).rgb * 0.25;

    return col;
}

// 提取亮部信息
half3 PreFilter(half3 color)
{
    half brightness = max(color.r, max(color.g, color.b));

    // 添加软裁剪
    half soft = brightness - _Threshold + _Knee;
    soft = clamp(soft, 0.0, _Knee * 2);
    soft = soft * soft / (_Knee * 4);

    half conteribution = max(soft, brightness - _Threshold);
    conteribution /= max(brightness, 0.00001);
    return color * conteribution;
}

float Luminance(float3 color) {
    return dot(color, float3(0.2126, 0.7152, 0.0722));
}



v2f vert(appdata v)
{
    v2f o;
    VertexPositionInputs PositionInputs = GetVertexPositionInputs(v.positionOS.xyz);
    o.positionCS = PositionInputs.positionCS;
    o.uv = v.texcoord;

    return o;
}


half4 PreFilterfrag(v2f i) : SV_Target
{

    half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
    half4 col = half4(PreFilter(tex).rgb, 1);
    return col;
}
          
half4 BloomPrefilterFirefiles(v2f i) : SV_Target {
    half3 color = 0.0;
    float weightSum = 0.0f;
    float2 offsets[] = {float2(0.0f, 0.0f), float2(-1.0f, -1.0f), float2(-1.0f, 1.0f), float2(1.0f, -1.0f), float2(1.0f, 1.0f)};

    for (int j = 0; j < 5; j++)
    {
        half3 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + offsets[j] * _MainTex_TexelSize.xy * 2.0);
        c = PreFilter(c);
        float w = 1.0 / (Luminance(c) + 1.0f);
        color += c * w;
        weightSum += w;
    }
    color /= weightSum;
    return half4(color, 1.0f);
}



half4 BoxBlurfrag(v2f i) : SV_Target
{

    half4 col = half4(BoxFliter(i.uv, _BlurRange).rgb, 1);

    return col;
}


half4 AddBlurfrag(v2f i) : SV_Target
{

    half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
    half4 col = half4(BoxFliter(i.uv, _BlurRange).rgb, 1);


    return col;
}

half4 Mergefrag(v2f i) : SV_Target
{

    half3 soure = SAMPLE_TEXTURE2D(_SourceTex, sampler_SourceTex, i.uv);
    half3 blur = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
    
    
    return float4(soure + blur, 1);
}

#endif