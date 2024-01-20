
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

struct v2f_DualBlurDown
{
    float2 uv[5] : TEXCOORD0;             // uv数组，
    float4 vertex : POSITION;             // 顶点
};

struct v2f_DualBlurUp
{
    float2 uv[8] : TEXCOORD0;
    float4 vertex : SV_POSITION;
};




struct v2f
{
    float2 uv : TEXCOORD0;
    float4 vertex : SV_POSITION;

};



// 获取高斯模糊，模糊范围
float4 GetBlurRange(float2 uv, float blurrange)
{

    float4 col = float4(0, 0, 0, 0);

    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv+ float2(0.0, 0.0)) * 0.147716f;
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(blurrange, 0.0)) * 0.118318f;
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(0.0, -blurrange)) * 0.118318f;
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(0.0, blurrange)) * 0.118318f;
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-blurrange, 0.0)) * 0.118318f;

    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(blurrange, blurrange)) * 0.0947416f;
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-blurrange, -blurrange)) * 0.0947416f;
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(blurrange, -blurrange)) * 0.0947416f;
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-blurrange, blurrange)) * 0.0947416f;

    return col;

}



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
    col = GetBlurRange(i.uv,blurrange);

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


//  

v2f_DualBlurDown DualKawaseDownvert(appdata v)
{
    //降采样
    v2f_DualBlurDown o;
    o.vertex = TransformObjectToHClip(v.positionOS.xyz);
    o.uv[0] = v.texcoord;


	//
    o.uv[1] = v.texcoord + float2(-1, -1) * (1 + _BlurRange) * _MainTex_TexelSize.xy * 0.5; //↖
    o.uv[2] = v.texcoord + float2(-1, 1) * (1 + _BlurRange) * _MainTex_TexelSize.xy * 0.5; //↙
    o.uv[3] = v.texcoord + float2(1, -1) * (1 + _BlurRange) * _MainTex_TexelSize.xy * 0.5; //↗
    o.uv[4] = v.texcoord + float2(1, 1) * (1 + _BlurRange) * _MainTex_TexelSize.xy * 0.5; //↘
    // 
    return o;
}



float4 DualKawaseDownfrag(v2f_DualBlurDown i) : SV_TARGET
{
    //降采样
    float4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv[0]) * 4;

    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv[1]);
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv[2]);
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv[3]);
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv[4]);
    
    return col * 0.125; //sum / 8.0f
}


v2f_DualBlurUp DualKawaseUpvert(appdata v)
{
    //升采样
    v2f_DualBlurUp o;
    o.vertex = TransformObjectToHClip(v.positionOS.xyz);
    o.uv[0] = v.texcoord;

	//
    o.uv[0] = v.texcoord + float2(-1, -1) * (1 + _BlurRange) * _MainTex_TexelSize.xy * 0.5;
    o.uv[1] = v.texcoord + float2(-1, 1) * (1 + _BlurRange) * _MainTex_TexelSize.xy * 0.5;
    o.uv[2] = v.texcoord + float2(1, -1) * (1 + _BlurRange) * _MainTex_TexelSize.xy * 0.5;
    o.uv[3] = v.texcoord + float2(1, 1) * (1 + _BlurRange) * _MainTex_TexelSize.xy * 0.5;
    o.uv[4] = v.texcoord + float2(-2, 0) * (1 + _BlurRange) * _MainTex_TexelSize.xy * 0.5;
    o.uv[5] = v.texcoord + float2(0, -2) * (1 + _BlurRange) * _MainTex_TexelSize.xy * 0.5;
    o.uv[6] = v.texcoord + float2(2, 0) * (1 + _BlurRange) * _MainTex_TexelSize.xy * 0.5;
    o.uv[7] = v.texcoord + float2(0, 2) * (1 + _BlurRange) * _MainTex_TexelSize.xy * 0.5;
    //
    return o;
}

float4 DualKawaseUpfrag(v2f_DualBlurUp i) : SV_TARGET
{
    //升采样
    float4 col = 0;

    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv[0]) * 2;
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv[1]) * 2;
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv[2]) * 2;
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv[3]) * 2;
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv[4]);
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv[5]);
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv[6]);
    col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv[7]);

    return col * 0.0833; //sum / 12.0f
}