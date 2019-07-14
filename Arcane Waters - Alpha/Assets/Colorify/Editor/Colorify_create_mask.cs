using UnityEngine;
using System.Collections;
using UnityEditor;
using System;
using System.IO;

public class Colorify_create_mask : MaterialEditor {

	private UnityEngine.Object[] materials;

	private void SaveTextureToFile(Texture2D texture,string folder,string name,bool jpg)
	{
		byte[] bytes;
		if (jpg)
			bytes = texture.EncodeToJPG();				
		else
			bytes = texture.EncodeToPNG();
		string filename;

		filename = folder + "/" + name;

		System.IO.File.WriteAllBytes(filename, bytes );
	}

	public void GenerateMask(bool jpg,bool bakeTexture)
	{
		Texture2D mainTex = (Texture2D)((Material)target).GetTexture("_MainTex");

		string assetPath = AssetDatabase.GetAssetPath(mainTex);

		TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath(assetPath);
		bool srgb = importer.sRGBTexture;

		Texture2D image = new Texture2D(mainTex.width, mainTex.height,TextureFormat.ARGB32,false,!bakeTexture || !srgb);

		string filename = Path.GetFileNameWithoutExtension(assetPath);
		string path = Path.GetDirectoryName(assetPath);


		if (bakeTexture)
			filename += "_recolor";
		else
			filename += "_mask";

		string ext;
		if (jpg)
			ext = "jpg";
		else
			ext = "png";

		string fullpath = EditorUtility.SaveFilePanel(
			"Save "+ (bakeTexture ? "texture" : "mask") + " as PNG",
			path,
			filename,
			ext).Replace("\\","/");

		if (fullpath == "")
		{
			GC.Collect();
			return;
		}
			
		string currentFolder = System.IO.Directory.GetCurrentDirectory().Replace("\\","/");


		if (!bakeTexture && fullpath.Substring(0,currentFolder.Length) != currentFolder)
		{
			EditorUtility.DisplayDialog("Invalid path","You can save mask only inside Assets directory.","OK");
			GC.Collect();
			return;
		}


		RenderTexture tempRT = RenderTexture.GetTemporary(mainTex.width,mainTex.height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);
		Material mat = null;
		if (bakeTexture)
			mat = new Material(Shader.Find("Hidden/Colorify_texture_baker"));
		else
			mat = new Material(Shader.Find("Hidden/Colorify_mask_creator"));
		mat.CopyPropertiesFromMaterial((Material)target);
		mat.SetFloat("_linear",bakeTexture && srgb && PlayerSettings.colorSpace == ColorSpace.Linear ? 1.0f : 0.0f);
		Graphics.Blit(mainTex,tempRT,mat);
		RenderTexture oldRT = RenderTexture.active;
		RenderTexture.active = tempRT;
		image.ReadPixels(new Rect(0, 0, tempRT.width, tempRT.height), 0, 0);
		RenderTexture.active = oldRT;
		RenderTexture.ReleaseTemporary(tempRT);	



		filename = Path.GetFileName(fullpath);
		path = Path.GetDirectoryName(fullpath);

		SaveTextureToFile(image,path,filename,jpg);

		if (fullpath.Substring(0,currentFolder.Length) == currentFolder)
		{
			string relativePath = fullpath.Substring(currentFolder.Length+1);

			AssetDatabase.ImportAsset(relativePath, ImportAssetOptions.Default);

			importer = (TextureImporter)TextureImporter.GetAtPath(relativePath);
			importer.sRGBTexture = bakeTexture && srgb;

			AssetDatabase.ImportAsset(relativePath, ImportAssetOptions.Default);

			if (!bakeTexture)
			{			
				Texture2D maskTex = (Texture2D)(AssetDatabase.LoadMainAssetAtPath(relativePath));
				if (maskTex != null)
				{
					((Material)target).SetTexture("_ColorifyMaskTex",maskTex);
					EditorUtility.DisplayDialog("Mask generation success.","Successfully generated recolor mask.","OK");
				}
				else
				{
					Debug.LogError("Error while generating recolor mask: could not load generated file.");
					EditorUtility.DisplayDialog("Error.","Could not load generated file.","OK");
				}
			}
		}



		GC.Collect();
	}

	public override void Awake ()
	{
		base.Awake();
		materials = new UnityEngine.Object[1];
		materials[0] = serializedObject.targetObject;
	}

	public override void OnInspectorGUI ()
	{
		serializedObject.Update ();
		materials[0] = serializedObject.targetObject;
		var theShader = serializedObject.FindProperty ("m_Shader"); 
		if (isVisible && !theShader.hasMultipleDifferentValues && theShader.objectReferenceValue != null)
		{
			EditorGUI.BeginChangeCheck();

			foreach(MaterialProperty mProp in GetMaterialProperties(materials))
			{
				ShaderProperty(mProp,mProp.displayName);
				GUILayout.Space(4);
				if (mProp.name == "_ColorifyMaskTex")
				{
					if (GUILayout.Button("Generate PNG mask"))
						GenerateMask(false,false);
					if (GUILayout.Button("Generate JPG mask"))
						GenerateMask(true,false);
					if (GUILayout.Button("Save texture as PNG"))
						GenerateMask(false,true);
					if (GUILayout.Button("Save texture as JPG"))
						GenerateMask(true,true);
				}
				GUILayout.Space(4);
			}
			
			if (EditorGUI.EndChangeCheck())
				PropertiesChanged ();
		}
	}
}
