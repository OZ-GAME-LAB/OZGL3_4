Shader "Sprites/Glow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0

        [Header(Glow)]
        [Toggle] _GlowEnabled ("Glow Enabled", Float) = 1
        _GlowInnerColor ("Glow Inner Color", Color) = (0.80, 0.74, 1.00, 1)
        _GlowOuterColor ("Glow Outer Color", Color) = (0.42, 0.33, 0.78, 1)
        _GlowSize ("Glow Size (pixel)", Range(0, 32)) = 8
        _GlowStrength ("Glow Strength", Range(0, 4)) = 1.2
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
        CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_instancing
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"

            float _GlowEnabled;
            fixed4 _GlowInnerColor;
            fixed4 _GlowOuterColor;
            float _GlowSize;
            float _GlowStrength;
            float4 _MainTex_TexelSize;

            // 주변을 살펴볼 방향 12개 (30도 간격)
            static const float2 GLOW_DIRECTIONS[12] =
            {
                float2( 1.000,  0.000), float2( 0.866,  0.500), float2( 0.500,  0.866),
                float2( 0.000,  1.000), float2(-0.500,  0.866), float2(-0.866,  0.500),
                float2(-1.000,  0.000), float2(-0.866, -0.500), float2(-0.500, -0.866),
                float2( 0.000, -1.000), float2( 0.500, -0.866), float2( 0.866, -0.500)
            };

            // 방향마다 살펴볼 거리 단계 수
            #define GLOW_STEPS 6

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 c = SampleSpriteTexture(IN.texcoord) * IN.color;

                if (_GlowEnabled < 0.5 || _GlowSize <= 0)
                {
                    c.rgb *= c.a;
                    return c;
                }

                // 주변 픽셀의 알파를 가우시안 가중치로 더해서 "캐릭터와 얼마나 가까운지"를 구합니다.
                float sigma = _GlowSize * 0.5;
                float glow = 0;
                float weightSum = 0;

                [unroll]
                for (int d = 0; d < 12; d++)
                {
                    [unroll]
                    for (int s = 1; s <= GLOW_STEPS; s++)
                    {
                        float dist = _GlowSize * s / GLOW_STEPS;
                        float2 offset = GLOW_DIRECTIONS[d] * dist * _MainTex_TexelSize.xy;
                        float weight = exp(-(dist * dist) / (2.0 * sigma * sigma));

                        glow += tex2D(_MainTex, IN.texcoord + offset).a * weight;
                        weightSum += weight;
                    }
                }

                // 가장자리 바로 바깥은 약 절반이 캐릭터 안쪽이라 0.5 근처가 나오므로 2배로 맞춥니다.
                glow = saturate(glow / weightSum * 2.0 * _GlowStrength);

                // 캐릭터에 가까울수록 밝은 라벤더, 멀수록 진한 보라
                fixed4 glowColor = lerp(_GlowOuterColor, _GlowInnerColor, glow);
                float glowAlpha = glow * glowColor.a;

                // 캐릭터를 글로우 위에 겹쳐서 그립니다. (프리멀티플라이드 알파)
                float3 rgb = c.rgb * c.a + glowColor.rgb * glowAlpha * (1 - c.a);
                float alpha = c.a + glowAlpha * (1 - c.a);

                return fixed4(rgb, alpha);
            }
        ENDCG
        }
    }
}