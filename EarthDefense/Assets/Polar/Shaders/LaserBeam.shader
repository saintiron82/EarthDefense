Shader "ShapeDefense/LaserBeam"
{
    Properties
    {
        [Header(Base Properties)]
        [MainColor]_BaseColor("Base Color", Color) = (1,1,1,1)
        [HideInInspector]_Color("Line Color", Color) = (1,1,1,1)
        [MainTexture]_BaseMap("Base Map", 2D) = "white" {}
        [HideInInspector]_MainTex("Main Tex", 2D) = "white" {}

        [Header(Neon Emission)]
        [HDR]_EmissionColor("Emission Color", Color) = (0,2,4,1)
        _EmissionIntensity("Emission Intensity", Range(0, 10)) = 2.0
        _EmissionMap("Emission Map", 2D) = "white" {}

        [Header(Fresnel Glow)]
        _FresnelPower("Fresnel Power", Range(0.1, 5.0)) = 2.0
        [HDR]_FresnelColor("Fresnel Color", Color) = (0,4,8,1)
        _FresnelIntensity("Fresnel Intensity", Range(0, 5)) = 1.5

        [Header(Animation)]
        _PulseSpeed("Pulse Speed", Range(0, 10)) = 2.0
        _PulseIntensity("Pulse Intensity", Range(0, 2)) = 0.5

        [Header(Gradient)]
        _CoreWidth("Core Width", Range(0.8, 1.0)) = 0.8
        _EdgeFalloff("Edge Falloff", Range(0.1, 2.0)) = 1.0

        [Header(Blend Mode)]
        [KeywordEnum(Alpha, Additive)] _BlendMode("Blend Mode", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalRenderPipeline"
        }
        LOD 100

        // Alpha Blending Pass
        Pass
        {
            Name "AlphaBlend"
            Tags { "LightMode" = "Universal2D" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _BLENDMODE_ALPHA _BLENDMODE_ADDITIVE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float4 color : COLOR;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_EmissionMap);
            SAMPLER(sampler_EmissionMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _Color;
                half4 _EmissionColor;
                half _EmissionIntensity;
                half4 _FresnelColor;
                half _FresnelPower;
                half _FresnelIntensity;
                half _PulseSpeed;
                half _PulseIntensity;
                half _CoreWidth;
                half _EdgeFalloff;
                float4 _BaseMap_ST;
                float4 _MainTex_ST;
                float4 _EmissionMap_ST;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Base texture sampling
                half4 texBase = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                half4 texMain = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, TRANSFORM_TEX(IN.uv, _MainTex));
                half4 emissionTex = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, TRANSFORM_TEX(IN.uv, _EmissionMap));

                // Pulse animation
                half pulse = sin(_Time.y * _PulseSpeed) * 0.5 + 0.5;
                pulse = lerp(1.0, 1.0 + _PulseIntensity, pulse);

                // Gradient from center to edge (assuming UV.x is along beam)
                half gradient = saturate(1.0 - abs(IN.uv.y - 0.5) * 2.0);
                gradient = pow(gradient, _EdgeFalloff);
                gradient = saturate((gradient - (1.0 - _CoreWidth)) / _CoreWidth);

                // Fresnel effect for edge glow
                float3 viewDirWS = normalize(GetCameraPositionWS() - IN.positionWS);
                half fresnel = 1.0 - saturate(dot(normalize(IN.normalWS), viewDirWS));
                fresnel = pow(fresnel, _FresnelPower) * _FresnelIntensity;

                // Base color
                half4 baseColor = texBase * texMain * _BaseColor * _Color * IN.color;

                // Emission calculation
                half3 emission = _EmissionColor.rgb * _EmissionIntensity * emissionTex.rgb;
                emission *= pulse * gradient;

                // Add fresnel glow
                emission += _FresnelColor.rgb * fresnel * pulse;

                // Final color
                half4 finalColor;
                finalColor.rgb = baseColor.rgb + emission;
                finalColor.a = baseColor.a * gradient;

                return finalColor;
            }
            ENDHLSL
        }

        // Additive Blending Pass
        Pass
        {
            Name "AdditiveBlend"
            Tags { "LightMode" = "Universal2D" }
            Blend One One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _BLENDMODE_ALPHA _BLENDMODE_ADDITIVE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float4 color : COLOR;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_EmissionMap);
            SAMPLER(sampler_EmissionMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _Color;
                half4 _EmissionColor;
                half _EmissionIntensity;
                half4 _FresnelColor;
                half _FresnelPower;
                half _FresnelIntensity;
                half _PulseSpeed;
                half _PulseIntensity;
                half _CoreWidth;
                half _EdgeFalloff;
                float4 _BaseMap_ST;
                float4 _MainTex_ST;
                float4 _EmissionMap_ST;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Base texture sampling
                half4 texBase = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                half4 texMain = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, TRANSFORM_TEX(IN.uv, _MainTex));
                half4 emissionTex = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, TRANSFORM_TEX(IN.uv, _EmissionMap));

                // Pulse animation
                half pulse = sin(_Time.y * _PulseSpeed) * 0.5 + 0.5;
                pulse = lerp(1.0, 1.0 + _PulseIntensity, pulse);

                // Gradient from center to edge
                half gradient = saturate(1.0 - abs(IN.uv.y - 0.5) * 2.0);
                gradient = pow(gradient, _EdgeFalloff);
                gradient = saturate((gradient - (1.0 - _CoreWidth)) / _CoreWidth);

                // Fresnel effect
                float3 viewDirWS = normalize(GetCameraPositionWS() - IN.positionWS);
                half fresnel = 1.0 - saturate(dot(normalize(IN.normalWS), viewDirWS));
                fresnel = pow(fresnel, _FresnelPower) * _FresnelIntensity;

                // Base color contribution
                half4 baseColor = texBase * texMain * _BaseColor * _Color * IN.color;

                // Emission for additive glow
                half3 emission = _EmissionColor.rgb * _EmissionIntensity * emissionTex.rgb;
                emission *= pulse * gradient;

                // Add fresnel glow
                emission += _FresnelColor.rgb * fresnel * pulse;

                // For additive pass, output emission + base contribution
                half4 finalColor;
                finalColor.rgb = (baseColor.rgb + emission) * gradient;
                finalColor.a = 1.0; // Alpha not needed for additive

                return finalColor;
            }
            ENDHLSL
        }
    }
}
