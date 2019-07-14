Shader "Colorify/Mask(baked)/1 color/Toon Lit" {
	Properties {
		_Color ("Main Color", Color) = (0.5,0.5,0.5,1)
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_ColorifyMaskTex ("Colorify mask (RGB)", 2D) = "black" {}
		_Sharpness("Sharpness", Range (0.01,1.0)) = 0.25
		_LightIntensity("Light intensity",Range (0.01,2.0)) = 1
		_PatCol ("Pattern Color", Color) = (1,1,1,1)
		_NewColor ("New Color", Color) = (1,1,1,1)		
	}

	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
CGPROGRAM
#pragma surface surf ToonRamp

half _Sharpness;
half _LightIntensity;

// custom lighting function that uses a texture ramp based
// on angle between light direction and normal
#pragma lighting ToonRamp exclude_path:prepass
inline half4 LightingToonRamp (SurfaceOutput s, half3 lightDir, half atten)
{
	#ifndef USING_DIRECTIONAL_LIGHT
	lightDir = normalize(lightDir);
	#endif
	
	//half d = dot (s.Normal, lightDir)*0.5 + 0.5;
	half d = dot (s.Normal, lightDir);	
	//half ramp = saturate(d);
	half ramp = saturate( round( d / _Sharpness) * _Sharpness);
	//half3 ramp = tex2D (_Ramp, float2(d,d)).rgb;
	
	half4 c;
	c.rgb = s.Albedo * _LightColor0.rgb * ramp * (atten * 2) * _LightIntensity;	
	c.a = 0;
	return c;
}


sampler2D _MainTex;
sampler2D _ColorifyMaskTex; 
fixed4 _Color;
fixed4 _PatCol;
fixed4 _NewColor;
half _Range;
half _HueRange;

struct Input {
	float2 uv_MainTex : TEXCOORD0;
};

void surf (Input IN, inout SurfaceOutput o) {
	half4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
	fixed4 mask = tex2D(_ColorifyMaskTex, IN.uv_MainTex); 
	c.rgb = lerp(c.rgb,(_NewColor.rgb - _PatCol.rgb + c.rgb),mask.r);
	o.Albedo = c.rgb;
	o.Alpha = c.a;
}
ENDCG

	} 

	Fallback "Diffuse"
}
