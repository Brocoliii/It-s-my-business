Shader "Custom/CustomerOutline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (1,1,1,1)
        _OutlineSize ("Outline Size", Range(1, 6)) = 2
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment SpriteFrag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 texcoord : TEXCOORD0;
                fixed4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            fixed4 _OutlineColor;
            float _OutlineSize;

            v2f SpriteVert(appdata_t input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.texcoord = input.texcoord;
                output.color = input.color * _Color;
                return output;
            }

            fixed4 SpriteFrag(v2f input) : SV_Target
            {
                float2 texel = _MainTex_TexelSize.xy * _OutlineSize;
                float alpha = tex2D(_MainTex, input.texcoord).a;

                // Keep the original sprite transparent. Only transparent pixels
                // next to an opaque sprite pixel become part of the outline.
                clip(0.01 - alpha);

                float surroundingAlpha = 0;
                surroundingAlpha = max(surroundingAlpha, tex2D(_MainTex, input.texcoord + float2(texel.x, 0)).a);
                surroundingAlpha = max(surroundingAlpha, tex2D(_MainTex, input.texcoord - float2(texel.x, 0)).a);
                surroundingAlpha = max(surroundingAlpha, tex2D(_MainTex, input.texcoord + float2(0, texel.y)).a);
                surroundingAlpha = max(surroundingAlpha, tex2D(_MainTex, input.texcoord - float2(0, texel.y)).a);
                surroundingAlpha = max(surroundingAlpha, tex2D(_MainTex, input.texcoord + texel).a);
                surroundingAlpha = max(surroundingAlpha, tex2D(_MainTex, input.texcoord - texel).a);
                surroundingAlpha = max(surroundingAlpha, tex2D(_MainTex, input.texcoord + float2(texel.x, -texel.y)).a);
                surroundingAlpha = max(surroundingAlpha, tex2D(_MainTex, input.texcoord + float2(-texel.x, texel.y)).a);
                surroundingAlpha = max(surroundingAlpha, tex2D(_MainTex, input.texcoord + float2(texel.x * 2, 0)).a);
                surroundingAlpha = max(surroundingAlpha, tex2D(_MainTex, input.texcoord - float2(texel.x * 2, 0)).a);
                surroundingAlpha = max(surroundingAlpha, tex2D(_MainTex, input.texcoord + float2(0, texel.y * 2)).a);
                surroundingAlpha = max(surroundingAlpha, tex2D(_MainTex, input.texcoord - float2(0, texel.y * 2)).a);
                surroundingAlpha = max(surroundingAlpha, tex2D(_MainTex, input.texcoord + float2(texel.x * 1.4, texel.y * 1.4)).a);
                surroundingAlpha = max(surroundingAlpha, tex2D(_MainTex, input.texcoord - float2(texel.x * 1.4, texel.y * 1.4)).a);
                surroundingAlpha = max(surroundingAlpha, tex2D(_MainTex, input.texcoord + float2(texel.x * 1.4, -texel.y * 1.4)).a);
                surroundingAlpha = max(surroundingAlpha, tex2D(_MainTex, input.texcoord + float2(-texel.x * 1.4, texel.y * 1.4)).a);

                clip(surroundingAlpha - 0.1);

                // Do not multiply by sampled alpha: that makes the outline
                // appear faded wherever the source sprite has soft antialiasing.
                return fixed4(_OutlineColor.rgb, _OutlineColor.a) * input.color;
            }
            ENDCG
        }
    }
}
