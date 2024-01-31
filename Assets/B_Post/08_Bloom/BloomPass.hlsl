#ifndef BLOOM_PASS_INCLUDED
#define BLOOM_PASS_INCLUDED



CBUFFER_START(UnityPerMaterial)
    float4 _BloomBlurSize;
    float4 _MainTex_TexelSize;
    float _BloomIntensity;
    float4 _BloomThreshold;
CBUFFER_END



TEXTURE2D(_MainTex);                          SAMPLER(sampler_MainTex);
TEXTURE2D(_BloomTexture);               SAMPLER(sampler_BloomTexture);

struct appdata
{
    float4 positionOS : POSITION;
    float2 texcoord : TEXCOORD0;
};


struct v2f
{
    float4 positionCS : SV_POSITION;  // 屏幕空间坐标
    float2 uv : TEXCOORD0;            // 原始UV坐标
};

struct GaussianV2f
{
    float4 positionCS : SV_POSITION;  // 屏幕空间坐标
    float2 uv : TEXCOORD0;            // 原始UV坐标
    float4 uvOffset1 : TEXCOORD1;     // 偏移后的UV坐标1
    float4 uvOffset2 : TEXCOORD2;     // 偏移后的UV坐标2
    UNITY_VERTEX_OUTPUT_STEREO
};


v2f Vertex(appdata v)
{
    v2f o;
    VertexPositionInputs PositionInputs = GetVertexPositionInputs(v.positionOS.xyz);
    o.positionCS = PositionInputs.positionCS;
    o.uv = v.texcoord;

    return o;
}

// ===================================================================================================================================================

// 提取亮部函数
float3 ApplyBloomThreshold(float3 color) {
    float brightness = Max3(color.r, color.g, color.b);
    float soft = brightness + _BloomThreshold.y;
    soft = clamp(soft, 0.0f, _BloomThreshold.z);
    soft = soft * soft * _BloomThreshold.w;
    float contribution = max(soft, brightness - _BloomThreshold.x);
    contribution /= max(brightness, 0.00001f);
    return color * contribution;
}

float Luminance(float3 color)
{
    return dot(color, float3(0.2126, 0.7152, 0.0722));
}


// ===================================================================================================================================================


// 高斯模糊 顶点阶段
GaussianV2f GaussianBlurPassVertex(appdata v)
{
    GaussianV2f o;
    VertexPositionInputs PositionInputs = GetVertexPositionInputs(v.positionOS.xyz);
    o.positionCS = PositionInputs.positionCS;
    o.uv = v.texcoord;

    // 计算UV偏移
    //float4 uvOffset = o.uv.xyxy + _BloomBlurSize.xyxy * _MainTex_TexelSize.xyxy;

    // 计算偏移后的UV坐标
    o.uvOffset1 = o.uv.xyxy + _BloomBlurSize.xyxy * float4(1.0f, 1.0f, -1.0f, -1.0f) * _MainTex_TexelSize.xyxy;
    o.uvOffset2 = o.uv.xyxy + _BloomBlurSize.xyxy * float4(1.0f, 1.0f, -1.0f, -1.0f) * 2.0f * _MainTex_TexelSize.xyxy;

    return o;
}



// 高斯模糊 片元阶段
half4 GaussianBlurPassFragment(GaussianV2f i) : SV_Target
{

    // 
    half3 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).rgb * 0.4026;

    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uvOffset1.xy).rgb * 0.2442;
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uvOffset1.zw).rgb * 0.2442;
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uvOffset2.xy).rgb * 0.0545;
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uvOffset2.zw).rgb * 0.0545;


    return half4(col, 1.0);
}


// ===================================================================================================================================================


// 合成图形
half4 BloomCombinePassFragment(v2f i) : SV_Target
{

    half3 lowRes = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).rgb;
    half3 highRes = SAMPLE_TEXTURE2D(_BloomTexture, sampler_BloomTexture, i.uv).rgb;

    half3 color = 0.0f;
    #if defined _BLOOMADDTIVE
    color = lowRes * _BloomIntensity + highRes;
    #else
    color = lerp(highRes, lowRes, saturate(_BloomIntensity));
    // lerp(highRes, lowRes, saturate(_BloomIntensity))
    #endif


    return half4(color, 1.0f);
}


half4 BloomPrefilterPass(v2f i) : SV_Target 
{

    half3 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).rgb;
    half3 color = ApplyBloomThreshold(tex);
    return half4(color, 1.0f);
}



// 过滤Fireflies（光斑)
half4 BloomPrefilterFirefilesPass(v2f i) : SV_Target
{
    // 初始化 
    half3 color = 0.0;
    float weightSum = 0.0f;

    // 定义偏移量数组
    float2 offsets[] = {float2(0.0f, 0.0f), float2(-1.0f, -1.0f), float2(-1.0f, 1.0f), float2(1.0f, -1.0f), float2(1.0f, 1.0f)};
    // 遍历偏移量数组
    for (int j = 0; j < 5; j++)
    {
        half2 uv = (i.uv + offsets[j] * _MainTex_TexelSize.xy * 2.0);
        half3 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).rgb;
        // 提取亮部函数
        c = ApplyBloomThreshold(c);
        // 计算权重  和 颜色值
        float w = 1.0 / (Luminance(c) + 1.0f);
        color += c * w;                     
        weightSum += w;
    }
    color /= weightSum;

    return half4(color, 1.0f);
}

half4 BloomScatterFinalPass(v2f i) : SV_Target 
{
    half3 lowRes = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).rgb;
    half3 highRes = SAMPLE_TEXTURE2D(_BloomTexture, sampler_BloomTexture, i.uv).rgb;
    // 将低分辨率（光扩散）加上高分辨率的低亮度光 来近似补偿能量损失
    lowRes += highRes - ApplyBloomThreshold(highRes);
    return float4(lerp(highRes, lowRes, _BloomIntensity), 1.0f);
}


#endif