// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Colorify/Real-time/Mobile/2 Colors/Unlit/Transparent" {
	Properties 
	{
		_Color ("Main Color", Color) = (1,1,1,1)
		_MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
		_ClipTex ("Clip (R)", 2D) = "white" {}
		_WaterHeight("Water Height", Range(0, 1)) = 0.0
		_WaterAlpha("Water Alpha", Range(0, 1)) = 0.0
		_Palette ("Palette", 2D) = "black" {}
		_Palette2 ("Palette2", 2D) = "black" {}
		_Threshold("Threshold", Float) = 0.05
		
		// Stencil 
		[Toggle(USE_HAT_STENCIL)]
		_UseHatStencil("Is Hat", Float) = 0
		
		_Stencil("Stencil Ref", Int) = 3
		[Enum(StencilOp)] _StencilOp ("Stencil Pass", Int) = 0
		[Enum(StencilOp)] _StencilFail ("Stencil Fail", Int) = 0

		[Enum(CompareFunction)] _StencilComp("Stencil Comp", Int) = 0

		[Toggle(SHOW_HAT_CLIPPING)]
		_ShowHatClipping("Show Hat Clipping", Int) = 0

		[Toggle(CLEAR_STENCIL)]
		_UseHatStencil("Clear Stencil", Float) = 0
	}

		SubShader
		{
			Tags {"Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent"}
			LOD 100

			Cull Off
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite Off

		Pass 
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile _ SHOW_HAT_CLIPPING

			#include "UnityCG.cginc"
			#include "Assets/Resources/Shaders/PaletteSwapPerSprite.cginc"
					
			struct appdata_t {
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				float2 texcoord : TEXCOORD0;
			};

			float _ShowHatClipping;

			sampler2D _MainTex;
			sampler2D _MainTex2;
			float4 _MainTex_ST;

			sampler2D _ClipTex;
			float4 _ClipTex_ST;

			float4 _MainTex_TexelSize;

			sampler2D _Palette;
			float4 _Palette_TexelSize;

			sampler2D _Palette2;
			float4 _Palette2_TexelSize;

			fixed4 _Color;

			uniform float _WaterHeight;
			uniform float _WaterAlpha;
			
			uniform float _Threshold;

			v2f vert(appdata_t v)
			{
				v2f o;

				o.vertex = UnityObjectToClipPos(v.vertex);
				o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);

				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 c = tex2D(_MainTex, i.texcoord);

				// Get the Y position of the pixel in object space. The 6 is the number of rows in the spritesheet.
				// As of writing this comment, Unity doesn't provide a way of accessing that value directly. Sprites don't use _MainTex_ST.
				fixed yPosition = frac(i.texcoord.y * 6);
				
				// Apply alpha and color changes if we're partially underwater
				const half3 waterColor = half3(.24, .39, .62);
				half isUnderwater = max(sign(_WaterHeight - yPosition), 0) * c.a;
				c.rgb = lerp(c.rgb, waterColor, isUnderwater * 0.9);
				c.a = lerp(c.a, lerp(0.0, c.a, _WaterAlpha), isUnderwater);
				
				// Modify colors using palettes
				fixed3 afterPaletteColor = swapPalette(c, _Palette, _Threshold);
				fixed isUnchanged = max(sign(distance(c, afterPaletteColor) - 0.001), 0.0);
				afterPaletteColor = lerp(afterPaletteColor, swapPalette(c, _Palette2, _Threshold), 1 - isUnchanged);
				
				c.rgb = afterPaletteColor.rgb;

				// We don't apply the color alpha until the end so we can have semi-transparent objects without clipping
				c.a *=  _Color.a;

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
