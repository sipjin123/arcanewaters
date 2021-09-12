// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Colorify/Real-time/Mobile/2 Colors/Unlit/Transparent-GUI-Masked"
{
	Properties
	{
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		_ClipTex("Clip (R)", 2D) = "white" {}

		_ColorMask ("Color Mask", Float) = 15

        _Color ("Main Color", Color) = (1,1,1,1)
        _MinAlpha ("Min Alpha", Range (0.0, 1.0)) = 0.0

		[Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0

		_Palette ("Palette", 2D) = "black" {}
		_Palette2 ("Palette2", 2D) = "black" {}
		_Threshold("Threshold", Float) = 0.05

		[Toggle(SHOW_HAT_CLIPPING)]
		_ShowHatClipping("Show Hat Clipping", Int) = 0
	}

	SubShader
	{
		Tags
		{ 
			"Queue"="Transparent" 
			"IgnoreProjector"="True" 
			"RenderType"="Transparent" 
			"PreviewType"="Plane"
			"CanUseSpriteAtlas"="True"
		}
		
		Stencil
		{
			Ref 1
			Comp LEqual
			Pass keep
		}

		Cull Off
		Lighting Off
		ZWrite Off
		ZTest [unity_GUIZTestMode]
		Blend SrcAlpha OneMinusSrcAlpha
		ColorMask [_ColorMask]

		Pass
		{
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			#include "UnityUI.cginc"
			#include "Assets/Resources/Shaders/PaletteSwapPerSprite.cginc"

			#pragma multi_compile __ UNITY_UI_ALPHACLIP
			
			struct appdata_t {
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				half2 texcoord : TEXCOORD0;
			};

			int _ShowHatClipping;
			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4 _MainTex_TexelSize;

			sampler2D _ClipTex;
			float4 _ClipTex_ST;

			sampler2D _Palette;
			float4 _Palette_TexelSize;

			sampler2D _Palette2;
			float4 _Palette2_TexelSize;

			fixed4 _Color;

            half _MinAlpha;
			float _Threshold;
		
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

				fixed3 afterPaletteColor = swapPalette(c, _Palette, _Threshold);
				fixed isUnchanged = max(sign(distance(c, afterPaletteColor) - 0.01), 0.0);
				afterPaletteColor = lerp(afterPaletteColor, swapPalette(c, _Palette2, _Threshold), 1 - isUnchanged);

				c.rgb = afterPaletteColor.rgb;

				// Conditionally apply hat clipmask
				fixed4 clipmask_c = tex2D(_ClipTex, i.texcoord);
				float originalPixelAlpha = c.a;
				float clippedPixelAlpha = lerp(0, c.a, clipmask_c.r);
				c.a = lerp(originalPixelAlpha, clippedPixelAlpha, _ShowHatClipping);

				return c;
			}
		ENDCG
		}
	}
}
