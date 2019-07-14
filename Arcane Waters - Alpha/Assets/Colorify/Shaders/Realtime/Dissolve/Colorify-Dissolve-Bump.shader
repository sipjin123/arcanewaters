Shader "Colorify/Real-time/Dissolve/Simple/Bumped" {
Properties {
	_Color ("Main Color", Color) = (1,1,1,1)
	_Dissolve ("Dissolve power", Range(0,1)) = 0.5
	_EdgeColor ("Edge Color", Color) = (1,0,0)
    _EdgeWidth ("Edge Width", Range(0,1)) = 0.1		
	_MainTex ("Base (RGB) Transparency (A)", 2D) = "white" {}	
	_BumpMap ("Normalmap", 2D) = "bump" {}	
	_PatCol ("Pattern Color (Dissolve)", Color) = (1,1,1,1)	
	_Range ("Range (Dissolve)", Range (0.0, 2.0)) = 0.01
	_HueRange ("Hue Range (Dissolve)", Range (0.0, 4.0)) = 0.1	
}
SubShader {
	Tags{ "Queue" = "Transparent" "RenderType"="Transparent"}	
	LOD 400
	
CGPROGRAM
#pragma surface surf Lambert alpha:blend 
#pragma target 3.0


sampler2D _MainTex;
sampler2D _BumpMap;
fixed4 _Color;
fixed4 _EdgeColor;
half _EdgeWidth;
half _Dissolve;
fixed4 _PatCol;
half _Range;
half _HueRange;

struct Input {
	float2 uv_MainTex;
	float2 uv_BumpMap;	
};

void surf (Input IN, inout SurfaceOutput o) {
	fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color.a;
	half hue = atan2(1.73205 * (c.g - c.b), 2 * c.r - c.g - c.b + 0.001);
	half targetHue = atan2(1.73205 * (_PatCol.g - _PatCol.b), 2 * _PatCol.r - _PatCol.g - _PatCol.b + 0.001);		
	o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
	o.Albedo = c.rgb;	
	int isClear = int((sqrt(saturate(1 - ((c.r - _PatCol.r)*(c.r - _PatCol.r) + (c.g - _PatCol.g)*(c.g - _PatCol.g) + (c.b - _PatCol.b)*(c.b - _PatCol.b)) / (_Range * _Range))
	                          * saturate(1.0 - min(abs(hue-targetHue),6.28319 - abs(hue-targetHue))/(_HueRange * _HueRange))))
					  - (1 - _Dissolve + _EdgeWidth)+0.99);
	int isAtLeastLine = int((sqrt(saturate(1 - ((c.r - _PatCol.r)*(c.r - _PatCol.r) + (c.g - _PatCol.g)*(c.g - _PatCol.g) + (c.b - _PatCol.b)*(c.b - _PatCol.b)) / (_Range * _Range))
	                          * saturate(1.0 - min(abs(hue-targetHue),6.28319 - abs(hue-targetHue))/(_HueRange * _HueRange))))
					  + _Dissolve-0.01);
	half4 altCol = lerp((c.r * 0.21 + c.g * 0.72 + c.b * 0.07) * _EdgeColor, 0.0, isClear);
	o.Albedo = lerp(o.Albedo, altCol, isAtLeastLine);
	o.Alpha = lerp(1.0, 0.0, isClear);
}
ENDCG
}

FallBack "Transparent/VertexLit"
}