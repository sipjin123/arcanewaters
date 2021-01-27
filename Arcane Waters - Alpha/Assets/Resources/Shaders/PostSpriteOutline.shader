Shader "Effects/PostSpriteOutline"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _OutlineColor("Outline Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
		Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
		LOD 100
	
		Cull Off 
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha 

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            
            fixed equals(float x, float y)
            {
                return 1.0 - abs(sign(x - y));
            }

            fixed and(float x, float y)
            {
                return x * y;
            }

            fixed or(float x, float y)
            {
                return min(x + y, 1.0);
            }
                        
            fixed greater_than(float x, float y)
            {
                return max(sign(x - y), 0.0);
            }

            fixed less_than(float x, float y)
            {
                return max(sign(y - x), 0.0);
            }

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
            float4 _MainTex_TexelSize;

            fixed4 _OutlineColor;
            fixed _PixelSize;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);                
                o.uv = v.uv;
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                
                // We only paint white colors and only in transparent areas
                float outlineWidth = _PixelSize;
                fixed4 outlineColor = _OutlineColor;

                fixed left = tex2D(_MainTex, i.uv + float2(-_MainTex_TexelSize.x * outlineWidth, 0)).a;
				fixed up = tex2D(_MainTex, i.uv + float2(0, _MainTex_TexelSize.y * outlineWidth)).a;
				fixed right = tex2D(_MainTex, i.uv + float2(_MainTex_TexelSize.x * outlineWidth, 0)).a;
				fixed down = tex2D(_MainTex, i.uv + float2(0, -_MainTex_TexelSize.y * outlineWidth)).a;

                // This pixel is an outline if it's transparent and any of the pixels around (4 directions only) it isn't transparent (1 = true, 0 = false)
                float isOutline = and(less_than(col.a, 0.15), or(greater_than(left, 0.15), or(greater_than(right, 0.15), or(greater_than(up, 0.15), greater_than(down, 0.15)))));

                // We should clip this pixel if it's not transparent or it's transparent and not an outline pixel (1 = true, 0 = false)
                float shouldClip = and(less_than(col.a, 0.15), 1 - isOutline);

                clip (-shouldClip);
                
                return lerp(col, outlineColor, isOutline);
            }
            ENDCG
        }
        
    }
    Fallback "Standard"
}
