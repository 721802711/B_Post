Shader "B_Post/VolumetricLightShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("_Color", color) = (1,1,1,1)
        _MaxStep ("_MaxStep",float) = 200      //设置最大步数
        _MaxDistance ("_MaxDistance",float) = 1000   //最大步进距离
        _LightIntensity ("_LightIntensity",float) = 0.01 //每次步进叠加的光照强度
        _StepSize ("_StepSize" , float) = 0.1	 //每次步进距离 
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"}
        ZWrite Off
		ZTest Always
		Cull Off


        HLSLINCLUDE

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"


        CBUFFER_START(UnityPerMaterial)
        float4 _MainTex_ST, _FinalTex_TexelSize;
        float4 _Color;
        half _Brightness;
        float _MaxDistance;
        float _MaxStep;
        float _StepSize;
        float _LightIntensity;

        float _BlurInt;

        float _SigmaS, _SigmaR;
        float _Radius;
        float4 _MainTex_TexelSize;
        float _BilaterFilterFactor;
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
            TEXTURE2D(_FinalTex);                         SAMPLER(sampler_FinalTex);
            TEXTURE2D(_CameraDepthTexture);               SAMPLER(sampler_CameraDepthTexture);

        ENDHLSL

        // 体积光
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag


            // 接收阴影所需关键字
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS                    //接受阴影
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE            //产生阴影
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _SHADOWS_SOFT                         //软阴影




            // 输入 UV和深度
            float4 GetTheWorldPos(float2 ScreenUV, float Depth)
            {
                    
                float3 ScreenPos = float3(ScreenUV, Depth);                                  // 获取屏幕空间位置
                float4 normalScreenPos = float4(ScreenPos * 2.0 - 1.0, 1.0);                 // 映射到屏幕中心点
                float4 ndcPos = mul(unity_CameraInvProjection, normalScreenPos);             // 计算到ndc空间下的位置
                ndcPos = float4(ndcPos.xyz / ndcPos.w, 1.0);                                 

                float4 sencePos = mul(unity_CameraToWorld, ndcPos * float4(1,1,-1,1));      // 反推世界空间位置
                sencePos = float4(sencePos.xyz, 1.0);
                return sencePos;
            }
            // 阴影函数
            float Getshadow(float3 posWorld)
            {
                float4 shadowCoord = TransformWorldToShadowCoord(posWorld);                  // 获取阴影坐标
                float shadow = MainLightRealtimeShadow(shadowCoord);
   
                return shadow;
            }

            v2f vert (appdata v)
            {
                v2f o;
                VertexPositionInputs  PositionInputs = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = PositionInputs.positionCS;   
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);

                return o;
            }


            half4 frag (v2f i) : SV_Target
            {

                float2 uv = i.uv;
                float depth = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, uv).r;
                depth = 1 - depth;   // 得到深度值



                float3 ro = _WorldSpaceCameraPos.xyz;                    // 相机在世界空间中的位置
                float3 worldPos = GetTheWorldPos(uv, depth).xyz;         // 屏幕纹理坐标和深度值重构出当前像素在世界空间中的位置



                float3 rd = normalize(worldPos - ro);     // 像素位置 到  摄像机位置
                float3 currentPos = ro;                   //  初始化一个 摄像机位置

                float m_length = min(length(worldPos - ro), _MaxDistance);      // 使用 length 函数计算出当前像素到相机之间的距离


                float delta = _StepSize;       // 步长大小  控制每次迭代的步长
                float totalInt = 0;
                float d = 0;


                // 光线进步计算
                for(int j = 0; j < _MaxStep; j++)
                {
                    d += delta;
                    if(d > m_length) break;   // 判断距离大于设定的距离 不在生成
                    currentPos += delta * rd;               // 根据步长 delta 和方向向量 rd 计算出当前像素的位置 currentPos
                    totalInt += _LightIntensity * Getshadow(currentPos);   // 然后使用 Getshadow 函数计算出当前像素位置的阴影值
                } 


                Light mylight = GetMainLight();                                //获取场景主光源
                half4 LightColor = half4(mylight.color,1);                     //获取主光源的颜色



                half3 lightCol = totalInt * LightColor * _Color.rgb * _Color.a;
                half3 oCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).rgb;
                half3 dCol = lightCol + oCol;                  //原图和 计算后的图叠加

                //return LightColor;
                return float4(lightCol, 1);
            }
            ENDHLSL
        }

    }

}
