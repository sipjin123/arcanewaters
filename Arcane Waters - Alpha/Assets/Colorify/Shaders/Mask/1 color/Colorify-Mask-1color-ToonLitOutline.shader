Shader "Colorify/Mask(baked)/1 color/Toon Lit Outline" {
	Properties {
		_Color ("Main Color", Color) = (0.5,0.5,0.5,1)
		_OutlineColor ("Outline Color", Color) = (0,0,0,1)
		_Outline ("Outline width", Range (.002, 0.03)) = .005
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_ColorifyMaskTex ("Colorify mask (RGB)", 2D) = "black" {}
		_Sharpness("Sharpness", Range (0.01,1.0)) = 0.25
		_LightIntensity("Light intensity",Range (0.01,2.0)) = 1
		_PatCol ("Pattern Color", Color) = (1,1,1,1)
		_NewColor ("New Color", Color) = (1,1,1,1)		
	}

	SubShader {
		Tags { "RenderType"="Opaque" }
		UsePass "Colorify/Mask(baked)/1 color/Toon Lit/FORWARD"
		UsePass "Hidden/Colorify Toon Basic Outline/OUTLINE"
	} 
	
	Fallback "Toon/Lit"
}
