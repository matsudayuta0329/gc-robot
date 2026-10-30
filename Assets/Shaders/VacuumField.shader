Shader "Custom/VacuumField"
{
    Properties
    {
        _NoiseTex ("Repeatable Noise", 2D) = "gray" {}
        _Color ("Edge Color", Color) = (0.35, 0.85, 1, 0.45)
        _MaxAlpha ("Maximum Alpha", Range(0, 0.5)) = 0.35
        _FresnelPower ("Fresnel Power", Range(0.5, 8)) = 3
        _NoiseAmplitude ("Normal Displacement", Range(0, 0.5)) = 0.08
        _NoiseScale ("Noise Scale", Float) = 3
        _NoiseSpeed ("Noise Speed", Float) = 0.35
        _ThresholdSpeed ("Threshold Speed", Float) = 0.65
        _TipFadeStart ("Tip Fade Start", Range(0, 1)) = 0.4
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionHCS : SV_POSITION; float3 normalWS : TEXCOORD0; float3 viewWS : TEXCOORD1; float2 uv : TEXCOORD2; float noise : TEXCOORD3; };
            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _NoiseTex_ST;
                half4 _Color;
                float _MaxAlpha;
                float _FresnelPower;
                float _NoiseAmplitude;
                float _NoiseScale;
                float _NoiseSpeed;
                float _ThresholdSpeed;
                float _TipFadeStart;
            CBUFFER_END
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float2 noiseUV = IN.uv * _NoiseScale + _Time.y * _NoiseSpeed;
                float noise = SAMPLE_TEXTURE2D_LOD(_NoiseTex, sampler_NoiseTex, noiseUV, 0).r;
                float threshold = 0.5 + 0.5 * sin(_Time.y * _ThresholdSpeed * 6.2831853);
                float displacement = (noise - threshold) * _NoiseAmplitude;
                float3 positionOS = IN.positionOS.xyz + IN.normalOS * displacement;
                float3 positionWS = TransformObjectToWorld(positionOS);
                OUT.positionHCS = TransformWorldToHClip(positionWS);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.viewWS = GetWorldSpaceViewDir(positionWS);
                OUT.uv = IN.uv;
                OUT.noise = noise;
                return OUT;
            }
            half4 frag(Varyings IN) : SV_Target
            {
                half fresnel = pow(1 - saturate(abs(dot(normalize(IN.normalWS), normalize(IN.viewWS)))), _FresnelPower);
                half tipFade = 1 - smoothstep(_TipFadeStart, 1, IN.uv.y);
                half alpha = min(_MaxAlpha, 0.5) * _Color.a * fresnel * tipFade;
                return half4(_Color.rgb, alpha);
            }
            ENDHLSL
        }
    }
}
