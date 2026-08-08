Shader "Custom/FogOfWar"
{
    Properties
    {
        _FogTex ("Fog Texture", 2D) = "white" {}
        _WorldOrigin ("World Origin (XZ)", Vector) = (0,0,0,0)
        _WorldSize ("World Size (XZ)", Vector) = (100,100,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_FogTex);
            SAMPLER(sampler_FogTex);
            float4 _WorldOrigin;
            float4 _WorldSize;

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionHCS = TransformWorldToHClip(OUT.positionWS);
                return OUT;
            }

            // 메시 UV에 전혀 의존하지 않고, 월드 XZ 좌표에서 직접 UV를 계산.
            // FogOfWar.cs가 텍스처에 굽는 좌표계와 1:1로 맞아떨어져서 뒤집힐 여지가 없음.
            float4 frag(Varyings IN) : SV_Target
            {
                float2 uv = (IN.positionWS.xz - _WorldOrigin.xy) / _WorldSize.xy;
                float4 fog = SAMPLE_TEXTURE2D(_FogTex, sampler_FogTex, uv);
                return float4(0, 0, 0, fog.a);
            }
            ENDHLSL
        }
    }
}
