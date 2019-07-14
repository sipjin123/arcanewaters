using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorifyToTexture 
{	
	public class ColorifyParameters
	{
		public bool twoColors;
		public Color patternColor;
		public Color newColor;
		public float range;
		public float hueRange;
		public Color patternColor2;
		public Color newColor2;
		public float range2;
		public float hueRange2;

		public Color multiplierColor;

	}

	protected static Material material
	{
		get
		{
			if (_material == null)
				_material = new Material(Shader.Find("Hidden/Colorify_texture_baker"));
			return _material;
		}
	}

	protected static Material _material;

	public static void ColorifyTexture(Texture2D input, RenderTexture output, ColorifyParameters parameters, bool linearCorrection = false)
	{
		Material mat = material;
		mat.SetFloat("_linear",linearCorrection ? 0.0f : 1.0f);
		mat.SetColor("_Color",parameters.multiplierColor);
		mat.SetColor("_PatCol",parameters.patternColor);
		mat.SetColor("_NewColor",parameters.newColor);
		mat.SetFloat("_Range",parameters.range);
		mat.SetFloat("_HueRange",parameters.hueRange);
		if (parameters.twoColors)
		{			
			mat.SetColor("_PatCol2",parameters.patternColor2);
			mat.SetColor("_NewColor2",parameters.newColor2);
			mat.SetFloat("_Range2",parameters.range2);
			mat.SetFloat("_HueRange2",parameters.hueRange2);
		}

		Graphics.Blit(input,output,mat);
	}

}
