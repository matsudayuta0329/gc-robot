Shader "Custom/GarbageShader"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}

        [Toggle]_IsDisplacement("Is Enable Vacuum", Float) = 0
        _TargetPos("Target Pos", Vector) = (0.0, 0.0, 0.0, 0.0)
        _MaxDisplacement("MaxDisplacement", Range(0.0, 5.0)) = 0.5
        _EffectiveRange("Effective Range", Range(0.0, 10.0)) = 2

        _Light("Light", Vector) = (0.0, 0.0, 1.0, 0.0)
        _ShadowColor("Shadow Color", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float4 _BaseMap_ST;

                float _IsDisplacement;
                float4 _TargetPos;
                float _MaxDisplacement;
                float _EffectiveRange;
                
                float4 _Light;
                half4 _ShadowColor;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                float3 stretchedPos = TransformObjectToWorld(IN.positionOS.xyz);
                float distance = length(_TargetPos - stretchedPos);
                
                //元の頂点のワールド位置に加算
                if(_IsDisplacement != 0)
                    stretchedPos = stretchedPos + (
                        clamp(lerp(_MaxDisplacement,0,distance / _EffectiveRange), 0, distance < _MaxDisplacement? distance: _MaxDisplacement) * //Vacuumまでの距離から移動量を計算
                        (_TargetPos - stretchedPos) / distance//頂点位置からVacuumまでの方向ベクトルを計算
                    );

                Varyings OUT;
                OUT.positionHCS = TransformWorldToHClip(stretchedPos);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.normal = IN.normal;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) - (_ShadowColor * (dot(IN.normal, normalize(_Light)) + 1) / 2);
                return color;
            }
            ENDHLSL
        }
    }
}
