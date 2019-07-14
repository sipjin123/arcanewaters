// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Colorify/Real-time/Mobile/2 Colors/Unlit/Transparent" {
Properties {
	_Color ("Main Color", Color) = (1,1,1,1)
	_MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
	_MainTex2 ("_MainTex2", 2D) = "white" {} 
	_PatCol ("Pattern Color", Color) = (1,1,1,1)
	_NewColor ("New Color", Color) = (1,1,1,1)
	_Range ("Range", Range (0.0, 2.0)) = 0.01	
	_PatCol2 ("Pattern Color 2", Color) = (1,1,1,1)
	_NewColor2 ("New Color 2", Color) = (1,1,1,1)
	_Range2 ("Range 2", Range (0.0, 2.0)) = 0.01
	_WaterHeight("Water Height", Range(0, 1)) = 0.0
	_WaterAlpha("Water Alpha", Range(0, 1)) = 0.0
}

SubShader {
	Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
	LOD 100
	
	Cull Off 
	ZWrite Off
	Blend SrcAlpha OneMinusSrcAlpha 
	
	Pass {  
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag	
			
			#pragma shader_feature SWAP_TEXTURE
			
			#include "UnityCG.cginc"

			struct appdata_t {
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				half2 texcoord : TEXCOORD0;
			};

			sampler2D _MainTex;
			sampler2D _MainTex2;
			float4 _MainTex_ST;
			
			fixed4 _Color;
			fixed4 _PatCol;
			fixed4 _NewColor;
			half _Range;			
			fixed4 _PatCol2;
			fixed4 _NewColor2;
			half _Range2;	
			half _SwapTex;
			uniform float _WaterHeight;
			uniform float _WaterAlpha;
			
			v2f vert (appdata_t v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				sampler2D mainTex = _MainTex;

				#ifdef SWAP_TEXTURE
					mainTex = _MainTex2;
				#endif

				fixed4 c = tex2D(mainTex, i.texcoord) * _Color;
				c.rgb = lerp(lerp(c.rgb,(_NewColor.rgb - _PatCol.rgb + c.rgb),
	                     saturate(1 - ((c.r - _PatCol.r)*(c.r - _PatCol.r) + (c.g - _PatCol.g)*(c.g - _PatCol.g) + (c.b - _PatCol.b)*(c.b - _PatCol.b)) / (_Range * _Range))),
					(_NewColor2.rgb - _PatCol2.rgb + c.rgb),
					saturate(1 - ((c.r - _PatCol2.r)*(c.r - _PatCol2.r) + (c.g - _PatCol2.g)*(c.g - _PatCol2.g) + (c.b - _PatCol2.b)*(c.b - _PatCol2.b)) / (_Range2 * _Range2)));

				if ((i.texcoord.y * 6) % 1 < _WaterHeight && c.a > 0) {
                    c.rgb = lerp(c.rgb, half3(.24, .39, .62), .90);
					c.a = lerp(0.0, c.a, _WaterAlpha);
                }

				return c;
			}
		ENDCG
	}
}

}
