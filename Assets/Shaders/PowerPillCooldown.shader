Shader "PacStudent/Power Pill Cooldown"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Progress ("Progress", Range(0, 1)) = 1
        _PendingAlpha ("Pending Opacity", Range(0, 1)) = 0.1
        [HideInInspector] _SpriteBounds ("Sprite Bounds", Vector) = (0, 0, 1, 1)
        [HideInInspector] _SweepSigns ("Sweep Signs", Vector) = (1, 1, 0, 0)
        [HideInInspector] _Color ("Tint", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "DisableBatching"="True" }
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            #pragma vertex CooldownVertex
            #pragma fragment CooldownFragment
            #pragma multi_compile_instancing

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
            };
            struct Varyings
            {
                COMMON_2D_OUTPUTS
                half4 color : COLOR;
                float2 sector : TEXCOORD1;
            };
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float4 _SpriteBounds;
                float4 _SweepSigns;
                float _Progress;
                float _PendingAlpha;
            CBUFFER_END

            Varyings CooldownVertex(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                SetUpSpriteInstanceProperties();
                float2 sector = (input.positionOS.xy - _SpriteBounds.xy) / max(_SpriteBounds.zw, 0.0001);
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                Varyings output = CommonUnlitVertex(input);
                output.color = input.color * _Color * unity_SpriteColor;
                output.sector = sector * _SweepSigns.xy;
                return output;
            }

            half4 CooldownFragment(Varyings input) : SV_Target
            {
                half4 color = CommonUnlitFragment(input, input.color);
                // Start at twelve o'clock and reveal clockwise, without modifying the atlas UVs.
                float angle = frac(atan2(input.sector.x, input.sector.y) / 6.28318530718 + 1.0);
                float revealed = _Progress >= 1.0 ? 1.0 : (_Progress > 0.0 && angle < _Progress ? 1.0 : 0.0);
                color.a *= lerp(_PendingAlpha, 1.0, revealed);
                return color;
            }
            ENDHLSL
        }
    }
}
