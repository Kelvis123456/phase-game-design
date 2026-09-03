Shader "PHASE/Echo"
{
    Properties
    {
        _MainTex          ("Sprite Texture", 2D) = "white" {}
        _EchoColor        ("Echo Color", Color) = (0.227, 1.0, 0.831, 1)
        _Opacity          ("Opacity", Range(0, 1)) = 0.65
        _EmissionIntensity("Emission Glow", Range(0, 2)) = 0.3
    }

    SubShader
    {
        Tags
        {
            "Queue"           = "Transparent"
            "RenderType"      = "Transparent"
            "RenderPipeline"  = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "EchoForwardLit"

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _EchoColor;
                float  _Opacity;
                float  _EmissionIntensity;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 color       : COLOR;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv          = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color       = IN.color;
                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);

                // Descartar píxeles transparentes del sprite
                clip(tex.a - 0.01);

                // Luminancia del píxel original
                float lum = dot(tex.rgb, float3(0.299, 0.587, 0.114));

                // Color base del eco modulado por luminancia
                float3 echoRGB = _EchoColor.rgb * lum;

                // Glow aditivo en zonas brillantes (lum alto = más glow)
                echoRGB += _EchoColor.rgb * _EmissionIntensity * (lum * lum);

                return float4(echoRGB, tex.a * _Opacity * IN.color.a);
            }
            ENDHLSL
        }
    }

    FallBack "Sprites/Default"
}
