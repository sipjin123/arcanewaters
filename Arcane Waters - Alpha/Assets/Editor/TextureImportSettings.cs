using UnityEngine;
using UnityEditor;
using System;

public class TextureImportSettings : AssetPostprocessor {
   void OnPreprocessTexture () {
      TextureImporter importer = assetImporter as TextureImporter;

      // We want sprites to be pixel perfect
      importer.textureCompression = TextureImporterCompression.Uncompressed;
      importer.mipmapEnabled = false;
      importer.filterMode = FilterMode.Point;

      // Sprites aren't showing correctly without the Full Rect setting
      TextureImporterSettings textureSettings = new TextureImporterSettings();
      importer.ReadTextureSettings(textureSettings);
      textureSettings.spriteMeshType = SpriteMeshType.FullRect;
      importer.SetTextureSettings(textureSettings);
   }
}