﻿using UnityEngine;

public class ColorPickerTester : MonoBehaviour 
{

    public Renderer pickerRenderer;
    public ColorPicker picker;

    public Color Color = Color.red;

	// Use this for initialization
	void Start () 
    {
        picker.onValueChanged.AddListener(color =>
        {
            pickerRenderer.material.color = color;
            Color = color;
        });

		pickerRenderer.material.color = picker.CurrentColor;

        picker.CurrentColor = Color;
    }
	
	// Update is called once per frame
	void Update () {
	
	}
}
