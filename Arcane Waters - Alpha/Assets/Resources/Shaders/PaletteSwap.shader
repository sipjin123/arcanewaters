Shader "Unlit/PaletteSwap"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Palette ("Palette", 2D) = "white" {}
        _Threshold("Threshold", Float) = 0.05
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "PaletteSwapPerSprite.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            sampler2D _Palette;
            float4 _MainTex_ST;
            float4 _Palette_TexelSize;
			float _Threshold;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

                // Minimal shader executing palette swap - plug this line (+ header) to enable palette swapping
                fixed3 swappedCol = swapPalette(col, _Palette, _Threshold);
                col.rgb = swappedCol.rgb;
                // Alternative version - directly return swapped colors without storing it
                //return swapPalette(col);

                return col;
            }
            ENDCG
        }
    }
}
