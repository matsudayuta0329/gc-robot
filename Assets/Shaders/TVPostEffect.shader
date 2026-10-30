Shader "Hidden/GcRobot/TVPostEffect"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "TV Post Effect"
            ZWrite Off ZTest Always Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            float4 _TVParameters;

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                float2 texel = _BlitTexture_TexelSize.xy;
                float2 offset = (uv - 0.5) * 2.0 * _TVParameters.z * texel;
                float2 edge = texel * 0.5;
                half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                color.r = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, clamp(uv + offset, edge, 1.0 - edge)).r;
                color.b = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, clamp(uv - offset, edge, 1.0 - edge)).b;
                float row = uv.y * _BlitTexture_TexelSize.w;
                float stripe = 0.5 + 0.5 * cos(row * 6.2831853 / _TVParameters.y);
                color.rgb *= 1.0 - stripe * _TVParameters.x;
                return color;
            }
            ENDHLSL
        }
    }
}
