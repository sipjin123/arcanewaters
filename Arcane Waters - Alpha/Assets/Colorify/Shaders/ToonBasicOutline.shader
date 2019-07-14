// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/Colorify Toon Basic Outline" {
	Properties {
		_Color ("Main Color", Color) = (.5,.5,.5,1)
		_OutlineColor ("Outline Color", Color) = (0,0,0,1)
		_Outline ("Outline width", Range (.002, 0.03)) = .005
		_MainTex ("Base (RGB)", 2D) = "white" { }
		_ColorifyMaskTex ("Colorify mask (RGB)", 2D) = "black" {}
		_ToonShade ("ToonShader Cubemap(RGB)", CUBE) = "" { }
		_PatCol ("Pattern Color", Color) = (1,1,1,1)
		_NewColor ("New Color", Color) = (1,1,1,1)
		_Range ("Range", Range (0.0, 2.0)) = 0.01
		_HueRange ("Hue Range", Range (0.0, 4.0)) = 0.1	
		_PatCol2 ("Pattern Color 2", Color) = (1,1,1,1)
		_NewColor2 ("New Color 2", Color) = (1,1,1,1)
		_Range2 ("Range 2", Range (0.0, 2.0)) = 0.01
		_HueRange2 ("Hue Range 2", Range (0.0, 4.0)) = 0.1
	}
	
	CGINCLUDE
	#include "UnityCG.cginc"
	
	struct appdata {
		float4 vertex : POSITION;
		float3 normal : NORMAL;
	};

	struct v2f {
		float4 pos : SV_POSITION;
		UNITY_FOG_COORDS(0)
		fixed4 color : COLOR;
	};
	
	uniform float _Outline;
	uniform float4 _OutlineColor;
	
	
	v2f vert(appdata v) {
		v2f o;
		o.pos = UnityObjectToClipPos(v.vertex);

		float3 norm   = normalize(mul ((float3x3)UNITY_MATRIX_IT_MV, v.normal));
		float2 offset = TransformViewToProjection(norm.xy);

		o.pos.xy += offset * o.pos.z * _Outline;
		o.color = _OutlineColor;
		UNITY_TRANSFER_FOG(o,o.pos);
		return o;
	}
	ENDCG

	SubShader {
		Tags { "RenderType"="Opaque" }
		UsePass "Hidden/Colorify Toon Basic/BASE"
		Pass {
			Name "OUTLINE"
			Tags { "LightMode" = "Always" }
			Cull Front
			ZWrite On
			ColorMask RGB
			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog
			
			sampler2D _ColorifyMaskTex; 
			fixed4 _PatCol;
			fixed4 _NewColor;
			half _Range;
			half _HueRange;
			fixed4 _PatCol2;
			fixed4 _NewColor2;
			half _Range2;
			half _HueRange2;
	
	
			fixed4 frag(v2f i) : SV_Target
			{
				UNITY_APPLY_FOG(i.fogCoord, i.color);
				return i.color;
			}
			ENDCG
		}
	}
	
	Fallback "Toon/Basic"
}
