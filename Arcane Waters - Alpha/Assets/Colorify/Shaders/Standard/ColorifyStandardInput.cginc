#ifndef UNITY_STANDARD_INPUT_INCLUDED
#define UNITY_STANDARD_INPUT_INCLUDED

#include "UnityCG.cginc"
#include "UnityShaderVariables.cginc"
#include "UnityInstancing.cginc"
#include "UnityStandardConfig.cginc"
#include "UnityPBSLighting.cginc" // TBD: remove
#include "UnityStandardUtils.cginc"

//---------------------------------------
// Directional lightmaps & Parallax require tangent space too
#if (_NORMALMAP || DIRLIGHTMAP_COMBINED || DIRLIGHTMAP_SEPARATE || _PARALLAXMAP)
	#define _TANGENT_TO_WORLD 1 
#endif

#if (_DETAIL_MULX2 || _DETAIL_MUL || _DETAIL_ADD || _DETAIL_LERP)
	#define _DETAIL 1
#endif

//---------------------------------------
half4		_Color;
half		_Cutoff;

sampler2D	_MainTex;
float4		_MainTex_ST;

sampler2D	_DetailAlbedoMap;
float4		_DetailAlbedoMap_ST;

sampler2D	_BumpMap;
half		_BumpScale;

sampler2D	_DetailMask;
sampler2D	_DetailNormalMap;
half		_DetailNormalMapScale;

sampler2D	_SpecGlossMap;
sampler2D	_MetallicGlossMap;
half		_Metallic;
half		_Glossiness;
half		_GlossMapScale;

sampler2D	_OcclusionMap;
half		_OcclusionStrength;

sampler2D	_ParallaxMap;
half		_Parallax;
half		_UVSec;

half4 		_EmissionColor;
sampler2D	_EmissionMap;

sampler2D	_ColorifyMaskTex;
half4 _PatCol;
half4 _NewColor;
half _Range;
half _HueRange;
half4 _PatCol2;
half4 _NewColor2;
half4 _NewSpecularColor;
half _ColorifyMultiplier;
half _NewGlossiness;
half _Range2;
half _HueRange2;
half4 _NewSpecularColor2;
half _ColorifyMultiplier2;
half _NewGlossiness2;
half _ColorifySpecStrength;


//-------------------------------------------------------------------------------------
// Input functions

struct VertexInput
{
	float4 vertex	: POSITION;
	half3 normal	: NORMAL;
	float2 uv0		: TEXCOORD0;
	float2 uv1		: TEXCOORD1;
#if defined(DYNAMICLIGHTMAP_ON) || defined(UNITY_PASS_META)
	float2 uv2		: TEXCOORD2;
#endif
#ifdef _TANGENT_TO_WORLD
	half4 tangent	: TANGENT;
#endif
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

float4 TexCoords(VertexInput v)
{
	float4 texcoord;
	texcoord.xy = TRANSFORM_TEX(v.uv0, _MainTex); // Always source from uv0
	texcoord.zw = TRANSFORM_TEX(((_UVSec == 0) ? v.uv0 : v.uv1), _DetailAlbedoMap);
	return texcoord;
}		

half DetailMask(float2 uv)
{
	return tex2D (_DetailMask, uv).a;
}

half3 Albedo(float4 texcoords)
{
	half3 albedo = _Color.rgb * tex2D (_MainTex, texcoords.xy).rgb;
	#if (SHADER_TARGET < 30)
	#else
		#if _COLORIFYMASKED
			half3 colmask = tex2D(_ColorifyMaskTex, texcoords.xy).rgb;
			#if _COLORIFYTWOCOLORS
				albedo = lerp(lerp(albedo,(_NewColor.rgb - _PatCol.rgb + albedo) * _ColorifyMultiplier,colmask.r),(_NewColor2.rgb - _PatCol2.rgb + albedo) * _ColorifyMultiplier2,colmask.g); 
			#else
				albedo = lerp(albedo,(_NewColor.rgb - _PatCol.rgb + albedo) * _ColorifyMultiplier,colmask.r); 
			#endif			
		#elif _COLORIFYMASKEDLUMINOSITY
			half3 colmask = tex2D(_ColorifyMaskTex, texcoords.xy).rgb;
			#if _COLORIFYTWOCOLORS
				albedo = lerp(lerp(albedo,_NewColor.rgb * colmask.b * 2 * _ColorifyMultiplier,colmask.r) ,_NewColor2.rgb * colmask.b * 2  * _ColorifyMultiplier2,colmask.g); 
			#else				
				albedo = lerp(albedo,_NewColor.rgb * colmask.b * 2 * _ColorifyMultiplier,colmask.r) ;
			#endif			
		#elif _COLORIFYMASKEDLUMINOSITYREALTIME		
			half3 colmask = tex2D(_ColorifyMaskTex, texcoords.xy).rgb;
			half brightness = 0.299*albedo.r + 0.587*albedo.g + 0.114*albedo.b;
			#if _COLORIFYTWOCOLORS
				albedo = lerp(lerp(albedo,_NewColor.rgb * brightness * 2 * _ColorifyMultiplier,colmask.r),_NewColor2.rgb * brightness * 2 * _ColorifyMultiplier2,colmask.g); 
			#else				
				albedo = lerp(albedo,_NewColor.rgb * brightness * 2 * _ColorifyMultiplier,colmask.r);
			#endif			
		#elif _COLORIFYREALTIME
			half hue = atan2(1.73205 * (albedo.g - albedo.b), 2 * albedo.r - albedo.g - albedo.b + 0.001);
			half targetHue = atan2(1.73205 * (_PatCol.g - _PatCol.b), 2 * _PatCol.r - _PatCol.g - _PatCol.b + 0.001); 
			#if _COLORIFYTWOCOLORS
				half targetHue2 = atan2(1.73205 * (_PatCol2.g - _PatCol2.b), 2 * _PatCol2.r - _PatCol2.g - _PatCol2.b + 0.001); 
				albedo = lerp(lerp(albedo,(_NewColor.rgb - _PatCol.rgb + albedo) * _ColorifyMultiplier,
							sqrt(saturate(1 - ((albedo.r - _PatCol.r)*(albedo.r - _PatCol.r) + (albedo.g - _PatCol.g)*(albedo.g - _PatCol.g) + (albedo.b - _PatCol.b)*(albedo.b - _PatCol.b)) / (_Range * _Range))
								* saturate(1.0 - min(abs(hue-targetHue),6.28319 - abs(hue-targetHue))/(_HueRange * _HueRange)))),
					(_NewColor2.rgb - _PatCol2.rgb + albedo)  * _ColorifyMultiplier2,
						sqrt(saturate(1.0 - ((albedo.r - _PatCol2.r)*(albedo.r - _PatCol2.r) + (albedo.g - _PatCol2.g)*(albedo.g - _PatCol2.g) + (albedo.b - _PatCol2.b)*(albedo.b - _PatCol2.b)) / (_Range2 * _Range2))
							* saturate(1.0 - min(abs(hue-targetHue2),6.28319 - abs(hue-targetHue2))/(_HueRange2 * _HueRange2)))); 				
			#else
				albedo = lerp(albedo,(_NewColor.rgb - _PatCol.rgb + albedo) * _ColorifyMultiplier,
						sqrt(saturate(1 - ((albedo.r - _PatCol.r)*(albedo.r - _PatCol.r) + (albedo.g - _PatCol.g)*(albedo.g - _PatCol.g) + (albedo.b - _PatCol.b)*(albedo.b - _PatCol.b)) / (_Range * _Range))
							* saturate(1.0 - min(abs(hue-targetHue),6.28319 - abs(hue-targetHue))/(_HueRange * _HueRange)))); 								
			#endif
		#endif
	#endif
	
#if _DETAIL
	#if (SHADER_TARGET < 30)
		// SM20: instruction count limitation
		// SM20: no detail mask
		half mask = 1; 
	#else
		half mask = DetailMask(texcoords.xy);
	#endif
	half3 detailAlbedo = tex2D (_DetailAlbedoMap, texcoords.zw).rgb;
	#if _DETAIL_MULX2
		albedo *= LerpWhiteTo (detailAlbedo * unity_ColorSpaceDouble.rgb, mask);
	#elif _DETAIL_MUL
		albedo *= LerpWhiteTo (detailAlbedo, mask);
	#elif _DETAIL_ADD
		albedo += detailAlbedo * mask;
	#elif _DETAIL_LERP
		albedo = lerp (albedo, detailAlbedo, mask);
	#endif
#endif	
	return albedo;
}

half Alpha(float2 uv)
{
#if defined(_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A)
	return _Color.a;
#else
	return tex2D(_MainTex, uv).a * _Color.a;
#endif
}		

half Occlusion(float2 uv)
{
#if (SHADER_TARGET < 30)
	// SM20: instruction count limitation
	// SM20: simpler occlusion
	return tex2D(_OcclusionMap, uv).g;
#else
	half occ = tex2D(_OcclusionMap, uv).g;
	return LerpOneTo (occ, _OcclusionStrength);
#endif
}

half4 SpecularGloss(float2 uv)
{
	half4 sg;
#ifdef _SPECGLOSSMAP
	sg = tex2D(_SpecGlossMap, uv.xy);
	#if (SHADER_TARGET < 30)
	#else
		half4 newsg = sg;
		#if _COLORIFYMASKED
			half3 colmask = tex2D(_ColorifyMaskTex, uv).rgb;
			#if _COLORIFYTWOCOLORS
				newsg = lerp(lerp(newsg,(_NewSpecularColor - _PatCol + newsg),colmask.r),(_NewSpecularColor2 - _PatCol2 + newsg),colmask.g); 
			#else
				newsg = lerp(newsg,(_NewSpecularColor - _PatCol + newsg),colmask.r); 
			#endif
		#elif _COLORIFYMASKEDLUMINOSITY
			half3 colmask = tex2D(_ColorifyMaskTex, uv).rgb;
			#if _COLORIFYTWOCOLORS
				newsg = lerp(lerp(newsg,_NewSpecularColor * colmask.b * 2,colmask.r),_NewSpecularColor2 * colmask.b * 2,colmask.g); 
			#else				
				newsg = lerp(newsg,_NewSpecularColor * colmask.b * 2,colmask.r); 			
			#endif
		#elif _COLORIFYMASKEDLUMINOSITYREALTIME		
			half3 colmask = tex2D(_ColorifyMaskTex, uv).rgb;
			half brightness = 0.299*newsg.r + 0.587*newsg.g + 0.114*newsg.b;
			#if _COLORIFYTWOCOLORS
				newsg = lerp(lerp(newsg,_NewSpecularColor * brightness * 2,colmask.r),_NewSpecularColor2 * brightness * 2,colmask.g); 
			#else				
				newsg = lerp(newsg,_NewSpecularColor * brightness * 2,colmask.r); 			
			#endif		
		#endif
		sg = lerp(sg,newsg,_ColorifySpecStrength);
	#endif
#else	
	sg = half4(_SpecColor.rgb, _Glossiness);
	#if _COLORIFYMASKED || _COLORIFYMASKEDLUMINOSITY || _COLORIFYMASKEDLUMINOSITYREALTIME	
		half3 colmask = tex2D(_ColorifyMaskTex, uv).rgb;	
		half4 newsg;
		#if _COLORIFYTWOCOLORS
			newsg = lerp(lerp(sg,_NewSpecularColor,colmask.r),_NewSpecularColor2,colmask.g);
		#else				
			newsg = lerp(sg,_NewSpecularColor,colmask.r); 			
		#endif
		sg = lerp(sg,newsg,_ColorifySpecStrength);
	#endif
#endif	
	return sg;
}

half2 MetallicGloss(float2 uv)
{
	half2 mg;
	
#ifdef _METALLICGLOSSMAP
	#ifdef _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
		mg.r = tex2D(_MetallicGlossMap, uv).r;
		mg.g = tex2D(_MainTex, uv).a;
	#else
		mg = tex2D(_MetallicGlossMap, uv).ra;
	#endif
	mg.g *= _GlossMapScale;
#else
	mg.r = _Metallic;
	#ifdef _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
		mg.g = tex2D(_MainTex, uv).a * _GlossMapScale;
	#else
		mg.g = _Glossiness;
	#endif
#endif
	return mg;
}

half3 Emission(float2 uv)
{
#ifndef _EMISSION
	return 0;
#else
	return tex2D(_EmissionMap, uv).rgb * _EmissionColor.rgb;
#endif
}

#ifdef _NORMALMAP
half3 NormalInTangentSpace(float4 texcoords)
{
	half3 normalTangent = UnpackScaleNormal(tex2D (_BumpMap, texcoords.xy), _BumpScale);
	// SM20: instruction count limitation
	// SM20: no detail normalmaps
#if _DETAIL && !defined(SHADER_API_MOBILE) && (SHADER_TARGET >= 30) 
	half mask = DetailMask(texcoords.xy);
	half3 detailNormalTangent = UnpackScaleNormal(tex2D (_DetailNormalMap, texcoords.zw), _DetailNormalMapScale);
	#if _DETAIL_LERP
		normalTangent = lerp(
			normalTangent,
			detailNormalTangent,
			mask);
	#else				
		normalTangent = lerp(
			normalTangent,
			BlendNormals(normalTangent, detailNormalTangent),
			mask);
	#endif
#endif
	return normalTangent;
}
#endif

float4 Parallax (float4 texcoords, half3 viewDir)
{
// D3D9/SM30 supports up to 16 samplers, skip the parallax map in case we exceed the limit
#define EXCEEDS_D3D9_SM3_MAX_SAMPLER_COUNT	(defined(LIGHTMAP_ON) && defined(DIRLIGHTMAP_SEPARATE) && defined(SHADOWS_SCREEN) && defined(_NORMALMAP) && \
											 defined(_EMISSION) && defined(_DETAIL) && (defined(_METALLICGLOSSMAP) || defined(_SPECGLOSSMAP)))

#if !defined(_PARALLAXMAP) || (SHADER_TARGET < 30) || (defined(SHADER_API_D3D9) && EXCEEDS_D3D9_SM3_MAX_SAMPLER_COUNT)
	// SM20: instruction count limitation
	// SM20: no parallax
	return texcoords;
#else
	half h = tex2D (_ParallaxMap, texcoords.xy).g;
	float2 offset = ParallaxOffset1Step (h, _Parallax, viewDir);
	return float4(texcoords.xy + offset, texcoords.zw + offset);
#endif

#undef EXCEEDS_D3D9_SM3_MAX_SAMPLER_COUNT
}
			
#endif // UNITY_STANDARD_INPUT_INCLUDED
