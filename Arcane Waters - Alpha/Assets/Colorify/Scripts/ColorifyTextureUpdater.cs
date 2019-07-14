using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorifyTextureUpdater : MonoBehaviour {

	public bool applyOnStart = true;
	public bool applyOnUpdate = false;

	public Texture2D source;
	public RenderTexture output;

	public bool twoColors;

	public Color patternColor = Color.white;
	public Color newColor = Color.white;
	public float range = 0.1f;
	public float hueRange = 0.1f;

	public Color patternColor2 = Color.white;
	public Color newColor2 = Color.white;
	public float range2 = 0.1f;
	public float hueRange2 = 0.1f;

	public Color multiplierColor = Color.white;

	protected ColorifyToTexture.ColorifyParameters parameters = new ColorifyToTexture.ColorifyParameters();

	// Use this for initialization
	void Start () {
		if (applyOnStart)
			Apply();
	}
	
	// Update is called once per frame
	void Update () {
		if (applyOnUpdate)
			Apply();
	}

	public void Apply()
	{
		parameters.twoColors = twoColors;
		parameters.patternColor = patternColor;
		parameters.newColor = newColor;
		parameters.range = range;
		parameters.hueRange = hueRange;

		parameters.multiplierColor = multiplierColor;

		if (twoColors)
		{
			parameters.patternColor2 = patternColor2;
			parameters.newColor2 = newColor2;
			parameters.range2 = range2;
			parameters.hueRange2 = hueRange2;
		}

		ColorifyToTexture.ColorifyTexture(source,output,parameters);
	}
}
