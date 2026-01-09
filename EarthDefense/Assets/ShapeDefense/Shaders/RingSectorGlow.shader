Shader "ShapeDefense/RingSectorGlow"
{
    Properties
    {
        [HDR]_BaseColor("Base Color (Curtain)", Color) = (0, 0.8, 0.8, 0.2)
        [HDR]_EdgeColor("Edge Color (Neon)", Color) = (0, 5, 5, 1)

        [Enum(Line_Y,0, Box_All,1)] _EdgeMode("Edge Mode", Float) = 0
        _EdgeWidth("Edge Width (UV)", Range(0.001, 0.5)) = 0.01
        _EdgePower("Edge Sharpness", Range(1, 20)) = 10
        _EdgeIntensity("Emission Strength", Range(1, 50)) = 20
        [Toggle] _InvertLine("Invert Line Position", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent+100"
            "RenderPipeline" = "UniversalRenderPipeline"
        }
        LOD 100

        Pass
        {
            Name "CurtainAndEdge"
            Tags { "LightMode" = "Universal2D" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _EdgeColor;
                float _EdgeWidth;
                float _EdgePower;
                float _EdgeIntensity;
                float _InvertLine;
                float _EdgeMode;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.uv = input.uv;
                return o;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float edgeFactor;
                if (_EdgeMode < 0.5)
                {
                    float targetUV = (_InvertLine > 0.5) ? (1.0 - input.uv.y) : input.uv.y;
                    edgeFactor = 1.0 - smoothstep(0.0, _EdgeWidth, targetUV);
                }
                else
                {
                    float2 distToEdge = min(input.uv, 1.0 - input.uv);
                    float minDist = min(distToEdge.x, distToEdge.y);
                    edgeFactor = 1.0 - smoothstep(0.0, _EdgeWidth, minDist);
                }

                float glow = pow(saturate(edgeFactor), _EdgePower);
                float3 emission = _EdgeColor.rgb * glow * _EdgeIntensity;

                // Base is premultiplied by its alpha; emission adds on top
                float3 color = _BaseColor.rgb * _BaseColor.a + emission;
                return float4(color, _BaseColor.a);
            }
            ENDHLSL
        }
    }
}