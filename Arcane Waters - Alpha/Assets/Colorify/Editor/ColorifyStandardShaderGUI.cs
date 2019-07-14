using System;
using UnityEngine;

namespace UnityEditor
{
	internal class ColorifyStandardShaderGUI : ShaderGUI
	{
		private enum WorkflowMode
		{
			Specular,
			Metallic,
			Dielectric
		}
	
		public enum BlendMode
		{
			Opaque,
			Cutout,
			Fade,		// Old school alpha-blending mode, fresnel does not affect amount of transparency
			Transparent // Physically plausible transparency mode, implemented as alpha pre-multiply
		}
	
		public enum ColorifyMode
		{
			Masked,
			Realtime,
			Masked_luminosity,
			Masked_luminosity_realtime
		}
		
		public enum ColorifyColorsMode
		{
			One_color,
			Two_colors
		}
	
		private static class Styles
		{
			public static GUIStyle optionsButton = "PaneOptions";
			public static GUIContent uvSetLabel = new GUIContent("UV Set");
			public static GUIContent[] uvSetOptions = new GUIContent[] { new GUIContent("UV channel 0"), new GUIContent("UV channel 1") };
	
			public static string emptyTootip = "";
			public static GUIContent albedoText = new GUIContent("Albedo", "Albedo (RGB) and Transparency (A)");
			public static GUIContent alphaCutoffText = new GUIContent("Alpha Cutoff", "Threshold for alpha cutoff");
			public static GUIContent specularMapText = new GUIContent("Specular", "Specular (RGB) and Smoothness (A)");
			public static GUIContent metallicMapText = new GUIContent("Metallic", "Metallic (R) and Smoothness (A)");
			public static GUIContent smoothnessText = new GUIContent("Smoothness", "");
			public static GUIContent normalMapText = new GUIContent("Normal Map", "Normal Map");
			public static GUIContent heightMapText = new GUIContent("Height Map", "Height Map (G)");
			public static GUIContent occlusionText = new GUIContent("Occlusion", "Occlusion (G)");
			public static GUIContent emissionText = new GUIContent("Emission", "Emission (RGB)");
			public static GUIContent detailMaskText = new GUIContent("Detail Mask", "Mask for Secondary Maps (A)");
			public static GUIContent detailAlbedoText = new GUIContent("Detail Albedo x2", "Albedo (RGB) multiplied by 2");
			public static GUIContent detailNormalMapText = new GUIContent("Normal Map", "Normal Map");
			public static GUIContent colorifyMaskText = new GUIContent("Recolor mask", "Color 1 (R), Color 2 (G), Brighntess(B)");
	
			public static string whiteSpaceString = " ";
			public static string primaryMapsText = "Main Maps";
			public static string secondaryMapsText = "Secondary Maps";
			public static string colorifyArea = "Colorify settings";
			public static string renderingMode = "Rendering Mode";		
			public static string colorifyMode = "Colorify mode";		
			public static string colorifyColorsMode = "Colorify colors mode";
			public static GUIContent emissiveWarning = new GUIContent ("Emissive value is animated but the material has not been configured to support emissive. Please make sure the material itself has some amount of emissive.");
			public static GUIContent emissiveColorWarning = new GUIContent ("Ensure emissive color is non-black for emission to have effect.");
			public static readonly string[] blendNames = Enum.GetNames (typeof (BlendMode));
			public static readonly string[] colorifyNames = Enum.GetNames (typeof (ColorifyMode));		
			public static readonly string[] colorifyColorNames = new string[2] {"1 color","2 colors"};
		}
	
		MaterialProperty blendMode = null;
		MaterialProperty albedoMap = null;
		MaterialProperty albedoColor = null;
		MaterialProperty alphaCutoff = null;
		MaterialProperty specularMap = null;
		MaterialProperty specularColor = null;
		MaterialProperty metallicMap = null;
		MaterialProperty metallic = null;
		MaterialProperty smoothness = null;
		MaterialProperty bumpScale = null;
		MaterialProperty bumpMap = null;
		MaterialProperty occlusionStrength = null;
		MaterialProperty occlusionMap = null;
		MaterialProperty heigtMapScale = null;
		MaterialProperty heightMap = null;
		//MaterialProperty emissionScaleUI = null;
		//MaterialProperty emissionColorUI = null;
		MaterialProperty emissionColorForRendering = null;
		MaterialProperty emissionMap = null;
		MaterialProperty detailMask = null;
		MaterialProperty detailAlbedoMap = null;
		MaterialProperty detailNormalMapScale = null;
		MaterialProperty detailNormalMap = null;
		MaterialProperty uvSetSecondary = null;
		MaterialProperty colorifyMode = null;
		MaterialProperty colorifyColorsMode = null;
		MaterialProperty colorifyMask = null;
		MaterialProperty colorifyPatCol1 = null;
		MaterialProperty colorifyNewCol1 = null;
		MaterialProperty colorifySpecCol1 = null;
		MaterialProperty colorifyMultiplier1 = null;
		MaterialProperty colorifyPatCol2 = null;
		MaterialProperty colorifyNewCol2 = null;
		MaterialProperty colorifySpecCol2 = null;
		MaterialProperty colorifyMultiplier2 = null;
		MaterialProperty colorifyRange = null;
		MaterialProperty colorifyHueRange = null;
		MaterialProperty colorifyRange2 = null;
		MaterialProperty colorifyHueRange2 = null;
		MaterialProperty colorifySpecularStrength = null;

	
		MaterialEditor m_MaterialEditor;
		WorkflowMode m_WorkflowMode = WorkflowMode.Specular;

	
		bool m_FirstTimeApply = true;
	
		public void FindProperties (MaterialProperty[] props)
		{
			blendMode = FindProperty ("_Mode", props);
			albedoMap = FindProperty ("_MainTex", props);
			albedoColor = FindProperty ("_Color", props);
			alphaCutoff = FindProperty ("_Cutoff", props);
			specularMap = FindProperty ("_SpecGlossMap", props, false);
			specularColor = FindProperty ("_SpecColor", props, false);
			metallicMap = FindProperty ("_MetallicGlossMap", props, false);
			metallic = FindProperty ("_Metallic", props, false);
			if (specularMap != null && specularColor != null)
				m_WorkflowMode = WorkflowMode.Specular;
			else if (metallicMap != null && metallic != null)
				m_WorkflowMode = WorkflowMode.Metallic;
			else
				m_WorkflowMode = WorkflowMode.Dielectric;
			smoothness = FindProperty ("_Glossiness", props);
			bumpScale = FindProperty ("_BumpScale", props);
			bumpMap = FindProperty ("_BumpMap", props);
			heigtMapScale = FindProperty ("_Parallax", props);
			heightMap = FindProperty("_ParallaxMap", props);
			occlusionStrength = FindProperty ("_OcclusionStrength", props);
			occlusionMap = FindProperty ("_OcclusionMap", props);
			//emissionScaleUI = FindProperty ("_EmissionScaleUI", props);
			//emissionColorUI = FindProperty ("_EmissionColorUI", props);
			emissionColorForRendering = FindProperty ("_EmissionColor", props);
			emissionMap = FindProperty ("_EmissionMap", props);
			detailMask = FindProperty ("_DetailMask", props);
			detailAlbedoMap = FindProperty ("_DetailAlbedoMap", props);
			detailNormalMapScale = FindProperty ("_DetailNormalMapScale", props);
			detailNormalMap = FindProperty ("_DetailNormalMap", props);
			uvSetSecondary = FindProperty ("_UVSec", props);
			colorifyMode = FindProperty ("_ColorifyMode", props);
			colorifyColorsMode = FindProperty ("_ColorifyColorsMode", props);
			colorifyMask = FindProperty ("_ColorifyMaskTex", props);
			colorifyPatCol1 = FindProperty ("_PatCol", props);
			colorifyNewCol1 = FindProperty ("_NewColor", props);
			colorifySpecCol1 = FindProperty ("_NewSpecularColor", props);
			colorifyMultiplier1 = FindProperty ("_ColorifyMultiplier", props);
			colorifyPatCol2 = FindProperty ("_PatCol2", props);
			colorifyNewCol2 = FindProperty ("_NewColor2", props);
			colorifySpecCol2 = FindProperty ("_NewSpecularColor2", props);
			colorifyMultiplier2 = FindProperty ("_ColorifyMultiplier2", props);
			colorifyRange = FindProperty("_Range",props);
			colorifyHueRange = FindProperty("_HueRange",props);
			colorifyRange2 = FindProperty("_Range2",props);
			colorifyHueRange2 = FindProperty("_HueRange2",props);
			colorifySpecularStrength = FindProperty("_ColorifySpecStrength",props);
		}
	
		public override void OnGUI (MaterialEditor materialEditor, MaterialProperty[] props)
		{
			FindProperties (props); // MaterialProperties can be animated so we do not cache them but fetch them every event to ensure animated values are updated correctly
			m_MaterialEditor = materialEditor;
			Material material = materialEditor.target as Material;
	
			ShaderPropertiesGUI (material);
	
			// Make sure that needed keywords are set up if we're switching some existing
			// material to a standard shader.
			if (m_FirstTimeApply)
			{
				SetMaterialKeywords (material, m_WorkflowMode);
				m_FirstTimeApply = false;
			}
		}
	
		public void ShaderPropertiesGUI (Material material)
		{
			// Use default labelWidth
			EditorGUIUtility.labelWidth = 0f;
	
			// Detect any changes to the material
			EditorGUI.BeginChangeCheck();
			{
				BlendModePopup();
	
				// Primary properties
				GUILayout.Label (Styles.primaryMapsText, EditorStyles.boldLabel);
				DoAlbedoArea(material);
				DoSpecularMetallicArea();
				m_MaterialEditor.TexturePropertySingleLine(Styles.normalMapText, bumpMap, bumpMap.textureValue != null ? bumpScale : null);
				m_MaterialEditor.TexturePropertySingleLine(Styles.heightMapText, heightMap, heightMap.textureValue != null ? heigtMapScale : null);
				m_MaterialEditor.TexturePropertySingleLine(Styles.occlusionText, occlusionMap, occlusionMap.textureValue != null ? occlusionStrength : null);
				DoEmissionArea(material);
				m_MaterialEditor.TexturePropertySingleLine(Styles.detailMaskText, detailMask);
				EditorGUI.BeginChangeCheck();
				m_MaterialEditor.TextureScaleOffsetProperty(albedoMap);
				if (EditorGUI.EndChangeCheck())
					emissionMap.textureScaleAndOffset = albedoMap.textureScaleAndOffset; // Apply the main texture scale and offset to the emission texture as well, for Enlighten's sake
	
				EditorGUILayout.Space();
				GUILayout.Label (Styles.colorifyArea, EditorStyles.boldLabel);
				DoColorifyArea(material);
				EditorGUILayout.Space();
	
				// Secondary properties
				GUILayout.Label(Styles.secondaryMapsText, EditorStyles.boldLabel);
				m_MaterialEditor.TexturePropertySingleLine(Styles.detailAlbedoText, detailAlbedoMap);
				m_MaterialEditor.TexturePropertySingleLine(Styles.detailNormalMapText, detailNormalMap, detailNormalMapScale);
				m_MaterialEditor.TextureScaleOffsetProperty(detailAlbedoMap);
				m_MaterialEditor.ShaderProperty(uvSetSecondary, Styles.uvSetLabel.text);
			}
			if (EditorGUI.EndChangeCheck())
			{
				foreach (var obj in blendMode.targets)
					MaterialChanged((Material)obj, m_WorkflowMode);
			}
		}
	
		internal void DetermineWorkflow(MaterialProperty[] props)
		{
			if (FindProperty("_SpecGlossMap", props, false) != null && FindProperty("_SpecColor", props, false) != null)
				m_WorkflowMode = WorkflowMode.Specular;
			else if (FindProperty("_MetallicGlossMap", props, false) != null && FindProperty("_Metallic", props, false) != null)
				m_WorkflowMode = WorkflowMode.Metallic;
			else
				m_WorkflowMode = WorkflowMode.Dielectric;
		}
	
		public override void AssignNewShaderToMaterial (Material material, Shader oldShader, Shader newShader)
		{
			if (material.HasProperty("_Emission"))
			{
				material.SetColor("_EmissionColor", material.GetColor("_Emission"));
			}


			base.AssignNewShaderToMaterial(material, oldShader, newShader);
	
			if (oldShader == null || !oldShader.name.Contains("Legacy Shaders/"))
			{
				SetupMaterialWithBlendMode(material, (BlendMode)material.GetFloat("_Mode"));
				return;
			} 
	
			BlendMode blendMode = BlendMode.Opaque;
			if (oldShader.name.Contains("/Transparent/Cutout/"))
			{
				blendMode = BlendMode.Cutout;
			}
			else if (oldShader.name.Contains("/Transparent/"))
			{
				// NOTE: legacy shaders did not provide physically based transparency
				// therefore Fade mode
				blendMode = BlendMode.Fade;
			}
			material.SetFloat("_Mode", (float)blendMode);
	
			DetermineWorkflow( MaterialEditor.GetMaterialProperties(new Material[] { material }) );
			MaterialChanged(material, m_WorkflowMode);
		}
	
		void BlendModePopup()
		{
			EditorGUI.showMixedValue = blendMode.hasMixedValue;
			var mode = (BlendMode)blendMode.floatValue;
	
			EditorGUI.BeginChangeCheck();
			mode = (BlendMode)EditorGUILayout.Popup(Styles.renderingMode, (int)mode, Styles.blendNames);
			if (EditorGUI.EndChangeCheck())
			{
				m_MaterialEditor.RegisterPropertyChangeUndo("Rendering Mode");
				blendMode.floatValue = (float)mode;
			}
	
			EditorGUI.showMixedValue = false;
		}
	
		void ColorifyModePopup()
		{
			EditorGUI.showMixedValue = colorifyMode.hasMixedValue;
			var mode = (ColorifyMode)colorifyMode.floatValue;
			
			EditorGUI.BeginChangeCheck();
			mode = (ColorifyMode)EditorGUILayout.Popup(Styles.colorifyMode, (int)mode, Styles.colorifyNames);
			if (EditorGUI.EndChangeCheck())
			{
				m_MaterialEditor.RegisterPropertyChangeUndo("Colorify mode");
	        	colorifyMode.floatValue = (float)mode;
	    	}
	    
	    	EditorGUI.showMixedValue = false;
		}
	
		void ColorifyColorsModePopup()
		{
			EditorGUI.showMixedValue = colorifyColorsMode.hasMixedValue;
			var mode = (ColorifyColorsMode)colorifyColorsMode.floatValue;
			
			EditorGUI.BeginChangeCheck();
			mode = (ColorifyColorsMode)EditorGUILayout.Popup(Styles.colorifyColorsMode, (int)mode, Styles.colorifyColorNames);
			if (EditorGUI.EndChangeCheck())
			{
				m_MaterialEditor.RegisterPropertyChangeUndo("Colorify colors mode");
				colorifyColorsMode.floatValue = (float)mode;
	    	}
	    
	    	EditorGUI.showMixedValue = false;
		}
	
		void DoColorifyArea(Material material)
		{		
			ColorifyModePopup();
			if (material.GetFloat("_ColorifyMode") == 0 || material.GetFloat("_ColorifyMode") == 2 || material.GetFloat("_ColorifyMode") == 3)
			{
				m_MaterialEditor.TexturePropertySingleLine(Styles.colorifyMaskText, colorifyMask);
            }
			EditorGUILayout.Separator();
			ColorifyColorsModePopup();		
			EditorGUILayout.Separator();
			if (material.GetFloat("_ColorifyMode") != 2 && material.GetFloat("_ColorifyMode") != 3)
				m_MaterialEditor.ColorProperty(colorifyPatCol1,"Pattern color");
			m_MaterialEditor.ColorProperty(colorifyNewCol1,"New color");
			m_MaterialEditor.RangeProperty(colorifyMultiplier1,"Multiplier");
			if (material.GetFloat("_ColorifyMode") != 1 && m_WorkflowMode == WorkflowMode.Specular)
				m_MaterialEditor.ColorProperty(colorifySpecCol1,"New Specular color");
			if (material.GetFloat("_ColorifyMode") == 1)
            {
				m_MaterialEditor.RangeProperty(colorifyRange,"RGB Range");
				m_MaterialEditor.RangeProperty(colorifyHueRange,"Hue Range");
			}
			if (material.GetFloat("_ColorifyColorsMode") == 1)
			{
				EditorGUILayout.Separator();
				if (material.GetFloat("_ColorifyMode") != 2 && material.GetFloat("_ColorifyMode") != 3)
					m_MaterialEditor.ColorProperty(colorifyPatCol2,"Pattern color 2");
				m_MaterialEditor.ColorProperty(colorifyNewCol2,"New color 2");
				m_MaterialEditor.RangeProperty(colorifyMultiplier2,"Multiplier 2");
				if (material.GetFloat("_ColorifyMode") != 1 && m_WorkflowMode == WorkflowMode.Specular)
					m_MaterialEditor.ColorProperty(colorifySpecCol2,"New Specular color 2");
				if (material.GetFloat("_ColorifyMode") == 1)
				{
					m_MaterialEditor.RangeProperty(colorifyRange2,"RGB Range 2");
					m_MaterialEditor.RangeProperty(colorifyHueRange2,"Hue Range 2");
                }
            }

		}
		
		void DoAlbedoArea(Material material)
		{
			m_MaterialEditor.TexturePropertySingleLine(Styles.albedoText, albedoMap, albedoColor);		
			if (((BlendMode)material.GetFloat("_Mode") == BlendMode.Cutout))
			{
				m_MaterialEditor.ShaderProperty(alphaCutoff, Styles.alphaCutoffText.text, MaterialEditor.kMiniTextureFieldLabelIndentLevel+1);
			}
		}
	
		void DoEmissionArea(Material material)
		{
			// Emission for GI?
			if (m_MaterialEditor.EmissionEnabledProperty())
			{
				bool hadEmissionTexture = emissionMap.textureValue != null;

				// Texture and HDR color controls
				m_MaterialEditor.TexturePropertyWithHDRColor(Styles.emissionText, emissionMap, emissionColorForRendering, false);

				// If texture was assigned and color was black set color to white
				float brightness = emissionColorForRendering.colorValue.maxColorComponent;
				if (emissionMap.textureValue != null && !hadEmissionTexture && brightness <= 0f)
					emissionColorForRendering.colorValue = Color.white;

				// change the GI flag and fix it up with emissive as black if necessary
				m_MaterialEditor.LightmapEmissionFlagsProperty(MaterialEditor.kMiniTextureFieldLabelIndentLevel, true);
			}
		} 
	
		void DoSpecularMetallicArea()
		{
			if (m_WorkflowMode == WorkflowMode.Specular)
			{
				if (specularMap.textureValue == null)
					m_MaterialEditor.TexturePropertyTwoLines(Styles.specularMapText, specularMap, specularColor, Styles.smoothnessText, smoothness);
				else
					m_MaterialEditor.TexturePropertySingleLine(Styles.specularMapText, specularMap);
				m_MaterialEditor.RangeProperty(colorifySpecularStrength,"Specular recolor strength");
			}
			else if (m_WorkflowMode == WorkflowMode.Metallic)
			{
				if (metallicMap.textureValue == null)
					m_MaterialEditor.TexturePropertyTwoLines(Styles.metallicMapText, metallicMap, metallic, Styles.smoothnessText, smoothness);
				else
					m_MaterialEditor.TexturePropertySingleLine(Styles.metallicMapText, metallicMap);
			}
		}
	
		public static void SetupMaterialWithBlendMode(Material material, BlendMode blendMode)
		{
			switch (blendMode)
			{
				case BlendMode.Opaque:
					material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
					material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
					material.SetInt("_ZWrite", 1);
					material.DisableKeyword("_ALPHATEST_ON");
					material.DisableKeyword("_ALPHABLEND_ON");
					material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
					material.renderQueue = -1;
					break;
				case BlendMode.Cutout:
					material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
					material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
					material.SetInt("_ZWrite", 1);
					material.EnableKeyword("_ALPHATEST_ON");
					material.DisableKeyword("_ALPHABLEND_ON");
					material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
					material.renderQueue = 2450;
					break;
				case BlendMode.Fade:
					material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
					material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
					material.SetInt("_ZWrite", 0);
					material.DisableKeyword("_ALPHATEST_ON");
					material.EnableKeyword("_ALPHABLEND_ON");
					material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
					material.renderQueue = 3000;
					break;
				case BlendMode.Transparent:
					material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
					material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
					material.SetInt("_ZWrite", 0);
					material.DisableKeyword("_ALPHATEST_ON");
					material.DisableKeyword("_ALPHABLEND_ON");
					material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
					material.renderQueue = 3000;
					break;
			}
		}
		
		// Calculate final HDR _EmissionColor (gamma space) from _EmissionColorUI (LDR, gamma) & _EmissionScaleUI (gamma)
		static Color EvalFinalEmissionColor(Material material)
		{
			return material.GetColor("_EmissionColorUI") * material.GetFloat("_EmissionScaleUI");
		}
	
		static bool ShouldEmissionBeEnabled (Color color)
		{
			return color.grayscale > (0.1f / 255.0f);
		}
	
		static void SetMaterialKeywords(Material material, WorkflowMode workflowMode)
		{
			// Note: keywords must be based on Material value not on MaterialProperty due to multi-edit & material animation
			// (MaterialProperty value might come from renderer material property block)
			SetKeyword (material, "_NORMALMAP", material.GetTexture ("_BumpMap") || material.GetTexture ("_DetailNormalMap"));
			SetKeyword (material, "_COLORIFYMASKED", material.GetFloat("_ColorifyMode") == 0);
			SetKeyword (material, "_COLORIFYREALTIME", material.GetFloat("_ColorifyMode") == 1);
			SetKeyword (material, "_COLORIFYMASKEDLUMINOSITY", material.GetFloat("_ColorifyMode") == 2);
			SetKeyword (material, "_COLORIFYMASKEDLUMINOSITYREALTIME", material.GetFloat("_ColorifyMode") == 3);
			SetKeyword (material, "_COLORIFYTWOCOLORS", material.GetFloat("_ColorifyColorsMode") == 1);
			if (workflowMode == WorkflowMode.Specular)
				SetKeyword (material, "_SPECGLOSSMAP", material.GetTexture ("_SpecGlossMap"));
			else if (workflowMode == WorkflowMode.Metallic)
				SetKeyword (material, "_METALLICGLOSSMAP", material.GetTexture ("_MetallicGlossMap"));
			SetKeyword (material, "_PARALLAXMAP", material.GetTexture ("_ParallaxMap"));
			SetKeyword (material, "_DETAIL_MULX2", material.GetTexture ("_DetailAlbedoMap") || material.GetTexture ("_DetailNormalMap"));
	
			bool shouldEmissionBeEnabled = ShouldEmissionBeEnabled (material.GetColor("_EmissionColor"));
			SetKeyword (material, "_EMISSION", shouldEmissionBeEnabled);
	
			// Setup lightmap emissive flags
			MaterialGlobalIlluminationFlags flags = material.globalIlluminationFlags;
			if ((flags & (MaterialGlobalIlluminationFlags.BakedEmissive | MaterialGlobalIlluminationFlags.RealtimeEmissive)) != 0)
			{
				flags &= ~MaterialGlobalIlluminationFlags.EmissiveIsBlack;
				if (!shouldEmissionBeEnabled)
					flags |= MaterialGlobalIlluminationFlags.EmissiveIsBlack;
	
				material.globalIlluminationFlags = flags;
			}
		}
	
		bool HasValidEmissiveKeyword (Material material)
		{
			// Material animation might be out of sync with the material keyword.
			// So if the emission support is disabled on the material, but the property blocks have a value that requires it, then we need to show a warning.
			// (note: (Renderer MaterialPropertyBlock applies its values to emissionColorForRendering))
			bool hasEmissionKeyword = material.IsKeywordEnabled ("_EMISSION");
			if (!hasEmissionKeyword && ShouldEmissionBeEnabled (emissionColorForRendering.colorValue))
				return false;
			else
				return true;
		}
	
		static void MaterialChanged(Material material, WorkflowMode workflowMode)
		{
			SetupMaterialWithBlendMode(material, (BlendMode)material.GetFloat("_Mode"));

			SetMaterialKeywords(material, workflowMode);
		}

	
		static void SetKeyword(Material m, string keyword, bool state)
		{
			if (state)
				m.EnableKeyword (keyword);
			else
				m.DisableKeyword (keyword);
		}
	}

} // namespace UnityEditor
