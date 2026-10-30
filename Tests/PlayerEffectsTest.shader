Shader "Hidden/PlayerEffectsTest"
{
    Properties
    {
        _NoiseTex ("Noise", 2D) = "white" {}
        _BaseMap ("Base", 2D) = "white" {}
        _MaxAlpha ("Alpha", Float) = 0.5
    }
    SubShader
    {
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            float4 vert(float4 position : POSITION) : SV_POSITION { return position; }
            float4 frag() : SV_Target { return float4(1, 1, 1, 1); }
            ENDHLSL
        }
    }
}
