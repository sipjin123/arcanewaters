// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Colorify/Real-time/Mobile/2 Colors/Unlit/Transparent-GUI"
{
	Properties
	{
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		
		[Enum(CompareFunction)]_StencilComp ("Stencil Comparison", Float) = 8
		_Stencil ("Stencil ID", Float) = 0
		[Enum(StencilOp)] _StencilOp ("Stencil Operation", Float) = 0
		_StencilWriteMask ("Stencil Write Mask", Float) = 255
		_StencilReadMask ("Stencil Read Mask", Float) = 255

		_ColorMask ("Color Mask", Float) = 15

        _Color ("Main Color", Color) = (1,1,1,1)
        //_PatCol ("Pattern Color", Color) = (1,1,1,1)
        //_NewColor ("New Color", Color) = (1,1,1,1)
        //_Range ("Range", Range (0.0, 2.0)) = 0.01	
        //_PatCol2 ("Pattern Color 2", Color) = (1,1,1,1)
        //_NewColor2 ("New Color 2", Color) = (1,1,1,1)
        //_Range2 ("Range 2", Range (0.0, 2.0)) = 0.01
        _MinAlpha ("Min Alpha", Range (0.0, 1.0)) = 0.0

		[Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0

		_Palette ("Palette", 2D) = "black" {}
		_Palette2 ("Palette2", 2D) = "black" {}
		_Threshold("Threshold", Float) = 0.05

		[Toggle(USE_HAT_STENCIL)]
		_UseHatStencil("Is Hat", Float) = 0
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
			Ref [_Stencil]
			Comp [_StencilComp]
			Pass [_StencilOp] 
			ReadMask [_StencilReadMask]
			WriteMask [_StencilWriteMask]
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
			#pragma multi_compile _ USE_HAT_STENCIL
			
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
			float4 _MainTex_TexelSize;

			sampler2D _Palette;
			float4 _Palette_TexelSize;

			sampler2D _Palette2;
			float4 _Palette2_TexelSize;

			fixed4 _Color;
			//fixed4 _PatCol;
			//fixed4 _NewColor;
			//half _Range;			
			//fixed4 _PatCol2;
			//fixed4 _NewColor2;
			//half _Range2;
            half _MinAlpha;
			float _Threshold;

			fixed isTransparent(fixed alpha) {
				return max(sign(0.5 - alpha), 0);
			}

			fixed isOpaque(fixed alpha) {
				return 1 - isTransparent(alpha);
			}

			fixed hasPixelsUp (float2 coord, int minPix) {			
				// Clip transparent pixels below the hat line that have non-transparent pixels above them								
				const int vpix = 16;
				int count = 0;

				[unroll(vpix)]
				for	(int j = 0; j < vpix; ++j) {
					float pixUp = tex2D(_MainTex, coord + fixed2(0, _MainTex_TexelSize.y * j)).a;
					count += isOpaque(pixUp);
				}

				float shouldClip = max(0, sign(count - minPix));

				return shouldClip;
			}

			fixed hasPixelsSides (float2 coord, int minPix) {			
				// Clip transparent pixels below the hat line that have non-transparent pixels above them
				const int hpix = 8;
				int count = 0;

				[unroll(hpix)]
				for	(int j = 1; j <= hpix; j++) {
					fixed2 left = coord + fixed2(_MainTex_TexelSize.x * j, 0);					
					fixed2 right = coord - fixed2(_MainTex_TexelSize.x * j, 0);

					fixed leftA = tex2D(_MainTex, left).a;
					fixed rightA = tex2D(_MainTex, right).a;

					count = min(1, count + min(1, isTransparent(leftA) * hasPixelsUp(left, 3) + isTransparent(rightA) * hasPixelsUp(right, 3)));
				}

				float shouldClip = (1 -  max(0, sign(1 - count)));

				return shouldClip;
			}
			
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
				//c.rgb = lerp(lerp(c.rgb,(_NewColor.rgb - _PatCol.rgb + c.rgb),
	   //                  saturate(1 - ((c.r - _PatCol.r)*(c.r - _PatCol.r) + (c.g - _PatCol.g)*(c.g - _PatCol.g) + (c.b - _PatCol.b)*(c.b - _PatCol.b)) / (_Range * _Range))),
				//	(_NewColor2.rgb - _PatCol2.rgb + c.rgb),
				//	saturate(1 - ((c.r - _PatCol2.r)*(c.r - _PatCol2.r) + (c.g - _PatCol2.g)*(c.g - _PatCol2.g) + (c.b - _PatCol2.b)*(c.b - _PatCol2.b)) / (_Range2 * _Range2)));
                
				// Modify colors using palettes

				// If this is the hat layer, we need to clip transparent pixels above the "bottom of the hat" line
				#ifdef USE_HAT_STENCIL
					float shouldClip = min(1, hasPixelsUp(i.texcoord, 3) + hasPixelsSides(i.texcoord, 2));
					clip(-shouldClip * isTransparent(c.a));
				#endif

				fixed3 afterPaletteColor = swapPalette(c, _Palette, _Threshold);
				fixed isUnchanged = max(sign(distance(c, afterPaletteColor) - 0.01), 0.0);
				afterPaletteColor = lerp(afterPaletteColor, swapPalette(c, _Palette2, _Threshold), 1 - isUnchanged);

				c.rgb = afterPaletteColor.rgb;

				return c;
			}
		ENDCG
		}
	}
}
