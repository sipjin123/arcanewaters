// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Colorify/Real-time/Mobile/2 Colors/Unlit/Transparent" {
	Properties 
	{
		_Color ("Main Color", Color) = (1,1,1,1)
		_MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
		_WaterHeight("Water Height", Range(0, 1)) = 0.0
		_WaterAlpha("Water Alpha", Range(0, 1)) = 0.0
		_Palette ("Palette", 2D) = "black" {}
		_Palette2 ("Palette2", 2D) = "black" {}
		_Threshold("Threshold", Float) = 0.05
		
		// Stencil 
		[Toggle(USE_HAT_STENCIL)]
		_UseHatStencil("Is Hat", Range(0,1)) = 0
		_StencilRef ("Stencil Ref", Int) = 3
		[Enum(StencilOp)] _StencilPass ("Stencil Pass", Int) = 0
		[Enum(CompareFunction)] _StencilCompare ("Stencil Comp", Int) = 0

		// Outline
		[Header(Outline)]
		[Toggle(OUTLINE_ENABLED)]
		_OutlineEnabled ("Outline Enabled", Range(0, 1)) = 0
		_OutlineColor ("Outline Color", Color) = (1, 1, 1, 1)		
	}

	SubShader 
	{			
		Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
		LOD 100
	
		Cull Off 
		ZWrite Off
		
		Pass 
		{
			Blend SrcAlpha OneMinusSrcAlpha 

			Stencil 
			{
				Ref [_StencilRef]
				Comp [_StencilCompare]
				Pass [_StencilPass]
			}

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag	
			#pragma multi_compile _ USE_HAT_STENCIL	

			#include "UnityCG.cginc"
			#include "Assets/Shaders/PaletteSwapPerSprite.cginc"
					
			struct appdata_t {
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				float2 texcoord : TEXCOORD0;
			};

			sampler2D _MainTex;
			sampler2D _MainTex2;
			float4 _MainTex_ST;

			float4 _MainTex_TexelSize;

			sampler2D _Palette;
			float4 _Palette_TexelSize;

			sampler2D _Palette2;
			float4 _Palette2_TexelSize;

			fixed4 _Color;

			uniform float _WaterHeight;
			uniform float _WaterAlpha;
			
			uniform float _Threshold;

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
				
				fixed4 c = tex2D(mainTex, i.texcoord) * _Color;
				
				// If this is the hat layer, we need to clip transparent pixels above the "bottom of the hat" line
				#ifdef USE_HAT_STENCIL	
					mainTex = _MainTex;
				
					c = tex2D(mainTex, i.texcoord);
							
					const int pixelsV = 8;
					int t = 0;
										
					for	(int j = 0; j < pixelsV; j++) {
						fixed d = tex2D(mainTex, i.texcoord - fixed2(0, _MainTex_TexelSize.y * (j + 1))).a;
						t += max(0, sign(0.5 - d));
					}

					float shouldClip = max(sign((t + 2) - pixelsV), 0.0);
					float isOpaque = max(sign(c.a - 0.5), 0);
					
					clip(-shouldClip);
				#endif

				if ((i.texcoord.y * 6) % 1 < _WaterHeight && c.a > 0) {
					c.rgb = lerp(c.rgb, half3(.24, .39, .62), .90);
					c.a = lerp(0.0, c.a, _WaterAlpha);
				}

				// Modify colors using palettes
				fixed3 afterPaletteColor = swapPalette(c, _Palette, _Threshold);
				fixed isUnchanged = max(sign(distance(c, afterPaletteColor) - 0.001), 0.0);
				afterPaletteColor = lerp(afterPaletteColor, swapPalette(c, _Palette2, _Threshold), 1 - isUnchanged);
				
				c.rgb = afterPaletteColor.rgb;
				
				return c;
			}
			ENDCG
		}		
	}

}
