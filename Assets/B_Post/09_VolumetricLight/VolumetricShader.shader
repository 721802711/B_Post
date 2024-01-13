Shader "B_Post/VolumetricShader"
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


                //return float4(worldPos,1);
                //定义光线起始点 到 摄像机， 
                float3 rd = normalize(worldPos - ro);     // 像素位置 到  摄像机位置
                float3 currentPos = ro;                   //  初始化一个 摄像机位置

                // 
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


                return float4(lightCol, 1);
                //return m_length;
                //return float4(dCol, 1);
            }
            ENDHLSL
        }

        // 高斯模糊
        Pass
        {

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            v2f vert (appdata v)
            {
                v2f o;
                VertexPositionInputs  PositionInputs = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = PositionInputs.positionCS;   
                o.uv = v.texcoord;

                return o;
            }


            half4 frag (v2f i) : SV_Target
            {

                float4 col = float4(0, 0, 0, 0);
                
                float blurrange = _BlurInt / 300;
                
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

            ENDHLSL
        }

        // 双边滤波

        Pass
        {

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag


            struct appdata_varyings
            {
                float4 positionOS : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f_varyings
            {
                float4 uv[4] : TEXCOORD0;
                float4 positionCS : SV_POSITION;
            };


            float LuminanceRGB(float3 color)
            {
                return dot(color, float3(0.2125, 0.7154, 0.07211));
            }

			float CompareColor(float4 col1, float4 col2)
			{
				float l1 = LuminanceRGB(col1.rgb);
				float l2 = LuminanceRGB(col2.rgb);
				return smoothstep(_BilaterFilterFactor, 1.0, 1.0 - abs(l1 - l2));
			}


            v2f_varyings vert (appdata_varyings v)
            {
                v2f_varyings o;
                VertexPositionInputs  PositionInputs = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = PositionInputs.positionCS;   

                float3 dir = float3(1,-1,0);
                float2 delta = _MainTex_TexelSize.xy * _BlurInt;
                o.uv[0] = float4(v.texcoord , v.texcoord + dir.xx * delta);
                o.uv[1] = float4(v.texcoord + dir.yy * delta, v.texcoord + dir.xx * 2 * delta);
                o.uv[2] = float4(v.texcoord + dir.yy * 2 * delta , v.texcoord + dir.xx * 3 * delta); 
                o.uv[3] = float4(v.texcoord + dir.yy *3 * delta ,0,0); 

                return o;
            }


            half4 frag (v2f_varyings i) : SV_Target
            {

				real4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv[0].xy);
				real4 col0a = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex,i.uv[0].zw);
				real4 col0b = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv[1].xy);
				real4 col1a = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv[1].zw);
				real4 col1b = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv[2].xy);
				real4 col2a = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv[2].zw);
				real4 col2b = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv[3].xy);

				half w = 0.37004405286;
				half w0a = CompareColor(col, col0a) * 0.31718061674;
				half w0b = CompareColor(col, col0b) * 0.31718061674;
				half w1a = CompareColor(col, col1a) * 0.19823788546;
				half w1b = CompareColor(col, col1b) * 0.19823788546;
				half w2a = CompareColor(col, col2a) * 0.11453744493;
				half w2b = CompareColor(col, col2b) * 0.11453744493;

				half3 result;
				result = w * col.rgb;
				result += w0a * col0a.rgb;
				result += w0b * col0b.rgb;
				result += w1a * col1a.rgb;
				result += w1b * col1b.rgb;
				result += w2a * col2a.rgb;
				result += w2b * col2b.rgb;

				result /= w + w0a + w0b + w1a + w1b + w2a + w2b;
				return float4(result , 1.0);

                // return BilateralFilter(i.uv);

            }

            ENDHLSL
        }

        // 合并两个图像
        Pass
        {

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            v2f vert (appdata v)
            {
                v2f o;
                VertexPositionInputs  PositionInputs = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = PositionInputs.positionCS;   
                o.uv = v.texcoord;

                return o;
            }

            half4 frag (v2f i) : SV_Target
            {

                half3 oCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).rgb;
                half3 lCol = SAMPLE_TEXTURE2D(_FinalTex, sampler_FinalTex, i.uv).rgb;

                half3 dCol = lCol + oCol;                  //原图和 计算后的图叠加


                return float4(dCol,1);
            }
            ENDHLSL

        }

        // 

    }

}
