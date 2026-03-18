Shader "FortuneValley/LotEdgeGlow"
{
    Properties
    {
        _GlowColor ("Glow Color", Color) = (1, 1, 1, 0.5)
        _PulseSpeed ("Pulse Speed", Float) = 1.5
        _PulseMin ("Pulse Min Alpha", Float) = 0.6
        _PulseMax ("Pulse Max Alpha", Float) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent-1"
        }

        Pass
        {
            Name "EdgeGlow"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            // Quads are flat planes — visible from both sides
            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // SRP Batcher compatible constant buffer
            CBUFFER_START(UnityPerMaterial)
                float4 _GlowColor;
                float _PulseSpeed;
                float _PulseMin;
                float _PulseMax;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Pulse alpha using sin wave remapped to [PulseMin, PulseMax]
                float wave = sin(_Time.y * _PulseSpeed);
                float t = (wave + 1.0) * 0.5; // remap -1..1 to 0..1
                float alpha = lerp(_PulseMin, _PulseMax, t);

                // Fade from opaque at bottom (uv.y=0) to transparent at top (uv.y=1)
                float verticalFade = 1.0 - input.uv.y;

                return half4(_GlowColor.rgb, _GlowColor.a * alpha * verticalFade);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
