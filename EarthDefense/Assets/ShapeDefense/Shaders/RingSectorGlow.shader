Shader "ShapeDefense/RingSectorGlow"
{
    Properties
    {
        [MainColor]_BaseColor("Base Color", Color) = (1,1,1,1)
        _EmissionColor("Emission Color", Color) = (0,1,1,1)
        _EmissionStrength("Emission Strength", Range(0,5)) = 1
        _RadiusOffset("Radius Offset", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalRenderPipeline" "Queue"="Transparent" }

        Pass
        {
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _EmissionColor;
                half _EmissionStrength;
                float _RadiusOffset;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                
                // Apply radius offset in vertex shader
                float3 pos = IN.positionOS.xyz;
                float radius = length(pos.xy);
                if (radius > 0.001)
                {
                    float2 dir = normalize(pos.xy);
                    pos.xy = dir * (radius + _RadiusOffset);
                }
                
                OUT.positionCS = TransformObjectToHClip(pos);
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Base color from vertex RGB * material base color
                half3 baseRgb = IN.color.rgb * _BaseColor.rgb;
                
                // Emission mask from vertex alpha (set by RingSectorMesh per damaged cell)
                half emissionMask = IN.color.a;
                
                // Add emission only where mask > 0 (damaged cells)
                half3 emission = _EmissionColor.rgb * _EmissionStrength * emissionMask;
                
                // Use vertex alpha for base visibility (add floor), material alpha as multiplier
                half alpha = saturate(emissionMask + 0.3) * _BaseColor.a;
                
                // Final color with combined alpha
                return half4(baseRgb + emission, alpha);
            }
            ENDHLSL
        }
    }
}