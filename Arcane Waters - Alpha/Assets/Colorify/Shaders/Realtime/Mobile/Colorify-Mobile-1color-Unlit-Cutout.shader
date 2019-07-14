// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Colorify/Real-time/Mobile/1 Color/Unlit/Cutout" {
Properties {
	_Color ("Main Color", Color) = (1,1,1,1)	
	_MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
	_Cutoff ("Alpha cutoff", Range(0,1)) = 0.5	
	_PatCol ("Pattern Color", Color) = (1,1,1,1)
	_NewColor ("New Color", Color) = (1,1,1,1)
	_Range ("Range", Range (0.0, 2.0)) = 0.01
}

SubShader {
	Tags {"Queue"="AlphaTest" "IgnoreProjector"="True" "RenderType"="TransparentCutout"}
	LOD 100
	
	Cull Off 
	Lighting Off
	
	Pass {  
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
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
			float4 _MainTex_ST;
			fixed _Cutoff;
			
			fixed4 _Color;
			fixed4 _PatCol;
			fixed4 _NewColor;
			half _Range;
			
			v2f vert (appdata_t v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 c = tex2D(_MainTex, i.texcoord) * _Color;
				clip(c.a - _Cutoff);
				c.rgb = lerp(c.rgb,(_NewColor.rgb - _PatCol.rgb + c.rgb),
	                saturate(1 - ((c.r - _PatCol.r)*(c.r - _PatCol.r) + (c.g - _PatCol.g)*(c.g - _PatCol.g) + (c.b - _PatCol.b)*(c.b - _PatCol.b)) / (_Range * _Range)));	
				return c;
			}
		ENDCG
	}
}

}
