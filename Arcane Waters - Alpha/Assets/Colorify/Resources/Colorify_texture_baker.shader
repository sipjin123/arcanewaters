Shader "Hidden/Colorify_texture_baker" 
{
	Properties 
	{		
		_MainTex ("Base (RGB)", 2D) = "white" {}	
	}

	SubShader 
	{
		Pass
		{
		
		CGPROGRAM
		#pragma vertex vert_img
		#pragma fragment frag
		#pragma target 3.0
		
		#include "UnityCG.cginc"
		
		sampler2D _MainTex;
		fixed4 _Color;
		fixed4 _PatCol;
		fixed4 _NewColor;	
		half _Range;
		half _HueRange;
		fixed4 _PatCol2;
		fixed4 _NewColor2;
		half _Range2;
		half _HueRange2;
		fixed _linear;
 
		float sRGB_encode_f(float F)
		{
			float lin;
			if(F <= 0.00313)
				lin = F * 12.92;
			else
				//lin = pow((F+0.055)/1.055, 2.4);
				lin = pow(1.055 * F,1/2.4) - 0.055;
			return lin;
		}
		
		float4 frag(v2f_img i) : COLOR
		{
			float4 c = tex2D(_MainTex, i.uv) * _Color;
			
			float hue = atan2(1.73205 * (c.g - c.b), 2 * c.r - c.g - c.b + 0.001);
			float targetHue = atan2(1.73205 * (_PatCol.g - _PatCol.b), 2 * _PatCol.r - _PatCol.g - _PatCol.b + 0.001);
			float targetHue2 = atan2(1.73205 * (_PatCol2.g - _PatCol2.b), 2 * _PatCol2.r - _PatCol2.g - _PatCol2.b + 0.001);	 
			
			float coef1 = saturate(1 - ((c.r - _PatCol.r)*(c.r - _PatCol.r) + (c.g - _PatCol.g)*(c.g - _PatCol.g) + (c.b - _PatCol.b)*(c.b - _PatCol.b)) / (_Range * _Range));
			float hueCoef1 = saturate(1.0 - min(abs(hue-targetHue),6.28319 - abs(hue-targetHue))/(_HueRange * _HueRange));
			float coef2 = saturate(1 - ((c.r - _PatCol2.r)*(c.r - _PatCol2.r) + (c.g - _PatCol2.g)*(c.g - _PatCol2.g) + (c.b - _PatCol2.b)*(c.b - _PatCol2.b)) / (_Range2 * _Range2));
			float hueCoef2 = saturate(1.0 - min(abs(hue-targetHue2),6.28319 - abs(hue-targetHue2))/(_HueRange2 * _HueRange2));
			
			float brightness = c.r * 0.21 + c.g * 0.72 + c.b * 0.07;
			
			
			fixed4 output = lerp(lerp(c,(_NewColor - _PatCol + c),sqrt(coef1 * hueCoef1)),(_NewColor2 - _PatCol2 + c),sqrt(coef2 * hueCoef2));
			
			if (_linear > 0.5)
			{			
				output.r = sRGB_encode_f(output.r);
				output.g = sRGB_encode_f(output.g);
				output.b = sRGB_encode_f(output.b);	
			}
			
			return output;		
		}

		ENDCG
		} 
	}
}